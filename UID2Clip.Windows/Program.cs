using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using CommandLine;
using PCSC;
using PCSC.Exceptions;
using PCSC.Iso7816;
using PCSC.Monitoring;
using PCSC.Utils;
using UidHelper;

namespace UID2Clip.Windows {
    internal enum OutputFormat {
        Hex,
        Dec
    };

    public class Program {
        public class Options {
            [Option("reader-type", Default = ReaderType.HID, Required = false, HelpText = "Use HID for keyboard emulating readers, use PCSC for PCSC readers.")]
            public ReaderType ReaderType { get; set; }

            [Option("uid-input-format", Default = UidFormat.Default, Required = false, HelpText = "Specify input format (**)")]
            public UidFormat UidInputFormat { get; set; }

            [Option("uid-output-format", Default = UidFormat.HEX, Required = false, HelpText = "Specify output format (**)")]
            public UidFormat UidOutputFormat { get; set; }
        }

        private static string _newUid = string.Empty;
        private static bool _dataAvailable = false;

        private static string FormatForOuput(string uid, OutputFormat format = OutputFormat.Hex, bool byteOrderReversed = false, int bits = 0, string separator = ":") {
            string result = string.Empty;
            var words = uid.Split('-');

            if (byteOrderReversed) {
                Array.Reverse(words);
            }

            if (format == OutputFormat.Hex) {
                result = string.Join(separator, words);
            } else {
                var hexString = string.Join("", words);
                result = Convert.ToUInt64(hexString, 16).ToString();
            }
            return result;
        }

        [STAThread]
        public static void Main(string[] args) {

            Console.WriteLine("This program will monitor all SmartCard readers or keyboard and send Uid's to Clipboard.");

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(o => {
                    if (o.ReaderType == ReaderType.PCSC) {
                        UsePCSCReader();
                    } else {
                        Console.WriteLine($"Input format {o.UidInputFormat}");
                        Console.WriteLine($"Output format {o.UidOutputFormat}");
                        if (o.UidInputFormat == UidFormat.Default) {
                            throw new Exception("Input format cannot be default for HID devices.");
                        }

                        while (true) {
                            string input = Console.ReadLine();
                            if (input == string.Empty) {
                                Console.WriteLine("Exiting");
                                return;
                            } else {
                                ulong keyId = UidHelper.UidHelper.ParseUid(o.UidInputFormat, input, '-');
                                string result = UidHelper.UidHelper.FormatUid(keyId, o.UidOutputFormat);
                                var latestClipboardEntry = Clipboard.GetText();
                                if (latestClipboardEntry != result) {
                                    Clipboard.SetText(result);
                                    Console.WriteLine($"Wrote {result} to the Clipboard");
                                }
                            }
                        }
                    }
                });
        }

        private static void UsePCSCReader() {
            // Retrieve the names of all installed readers.
            var readerNames = GetReaderNames();

            if (IsEmpty(readerNames)) {
                Console.WriteLine("There are currently no readers installed.");
                Console.ReadKey();
                return;
            }

            // Create smart-card monitor using a context factory. 
            // The context will be automatically released after monitor.Dispose()
            using (var monitor = MonitorFactory.Instance.Create(SCardScope.System)) {
                AttachToAllEvents(monitor); // Remember to detach, if you use this in production!

                ShowUserInfo(readerNames);

                monitor.Start(readerNames);

                // Let the program run until the user presses CTRL-Q
                while (true) {
                    if (_dataAvailable) {
                        var latestClipboardEntry = Clipboard.GetText();
                        var formattedUid = FormatForOuput(_newUid, OutputFormat.Hex, false, 0, string.Empty);
                        if (latestClipboardEntry != formattedUid) {
                            Clipboard.SetText(formattedUid);
                            Console.WriteLine($"Wrote {formattedUid} to the Clipboard");
                        }
                        _dataAvailable = false;
                    }

                    if (Console.KeyAvailable) {
                        var key = Console.ReadKey();
                        if (ExitRequested(key)) {
                            break;
                        }

                        if (monitor.Monitoring) {
                            monitor.Cancel();
                            Console.WriteLine("Monitoring paused. (Press CTRL-Q to quit)");
                        } else {
                            monitor.Start(readerNames);
                            Console.WriteLine("Monitoring started. (Press CTRL-Q to quit)");
                        }
                    } else {
                        Thread.Sleep(250);
                    }
                }
            }
        }

        private static void ShowUserInfo(IEnumerable<string> readerNames) {
            foreach (var reader in readerNames) {
                Console.WriteLine($"Start monitoring for reader {reader}.");
            }

            Console.WriteLine("Press Ctrl-Q to exit or any key to toggle monitor.");
        }

        private static void AttachToAllEvents(ISCardMonitor monitor) {
            // Point the callback function(s) to the anonymous & static defined methods below.
            monitor.CardInserted += (sender, args) => CardArrived("CardInserted", args);
            monitor.CardRemoved += (sender, args) => CardRemoved("CardRemoved", args);
            monitor.MonitorException += MonitorException;
        }

        [STAThread]
        private static void CardArrived(object sender, CardStatusEventArgs args) {
            _newUid = GetUidString(args.ReaderName);
            _dataAvailable = true;
        }

        private static void CardRemoved(string eventName, CardStatusEventArgs unknown) {
        }

        private static void MonitorException(object sender, PCSCException ex) {
            Console.WriteLine("Monitor exited due an error:");
            Console.WriteLine(SCardHelper.StringifyError(ex.SCardError));
        }

        private static string[] GetReaderNames() {
            using (var context = ContextFactory.Instance.Establish(SCardScope.System)) {
                return context.GetReaders();
            }
        }

        private static bool ExitRequested(ConsoleKeyInfo key) =>
            key.Modifiers == ConsoleModifiers.Control &&
            key.Key == ConsoleKey.Q;

        private static bool IsEmpty(ICollection<string> readerNames) => readerNames == null || readerNames.Count < 1;

        private static string GetUidString(string readerName) {
            var uid = string.Empty;

            using (var context = ContextFactory.Instance.Establish(SCardScope.System)) {
                using (var rfidReader = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any)) {
                    var apdu = new CommandApdu(IsoCase.Case2Short, rfidReader.Protocol) {
                        CLA = 0xFF,
                        Instruction = InstructionCode.GetData,
                        P1 = 0x00,
                        P2 = 0x00,
                        Le = 0 // We don't know the ID tag size
                    };

                    using (rfidReader.Transaction(SCardReaderDisposition.Leave)) {
                        var sendPci = SCardPCI.GetPci(rfidReader.Protocol);
                        var receivePci = new SCardPCI(); // IO returned protocol control information.

                        var receiveBuffer = new byte[256];
                        var command = apdu.ToArray();

                        var bytesReceived = rfidReader.Transmit(
                            sendPci, // Protocol Control Information (T0, T1 or Raw)
                            command, // command APDU
                            command.Length,
                            receivePci, // returning Protocol Control Information
                            receiveBuffer,
                            receiveBuffer.Length); // data buffer

                        var responseApdu =
                            new ResponseApdu(receiveBuffer, bytesReceived, IsoCase.Case2Short, rfidReader.Protocol);

                        if (responseApdu.HasData) {
                            uid = BitConverter.ToString(responseApdu.GetData());
                        }
                    }
                }
                return uid;
            }
        }
    }
}

# Mifare Uid reading

This tool will read Mifare Uid's from either a PCSC card reader or a keyboard
emulating reader.  
The tool will be able to convert between common Uid representations.

# ER10-X Reader

This reader acts like a HID-USB device (keyboard)


|Format   	| Card 1  	| Card 2  	| HEX 1  	| HEX 2  	|
|---	|---	|---	|---	|---	|
| SPC PACE   	| 166949742336	  	| 579012736512	  	| 26DEFAE700  	| 86CFD93600  	|
| SPC WIEGAND  	| 3891977766  	| 920244102  	| E7FADE26  	| 36D9CF86  	|
| SPC AR618X  	| 3891977766  	| 920244102  	| E7FADE26  	| 36D9CF86  	|
| ACT Mifare Serial  	| 0652147431  	| 2261768502  	| 26DEFAE7  	| 86CFD936  	|
| ACT Mifare REV Serial  	| 3891977766  	| 0920244102  	| E7FADE26  	| 36D9CF86  	|
| ACT Mifare Sector  	|   	|   	|   	|   	|
| OMNIS  	| 3891977766  	| 0920244102  	| E7FADE26  	| 36D9CF86  	|

# PCSC API (Lenovo NFC):

| Card 1    | Card 2   |
|---    |---    |
| 26DEFAE7  | 86CFD936  |



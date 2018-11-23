# Danfoss-BLE
Sources for control Danfoss ECO via cc2541 as gateway

Contains MS Visual Studio 2012 PC project and IAR Embedded Workbench 10.10 TI cc2541 project.

cc2541 should be connected to PC via USB TTL converter.

Main things I've found out:
1) Before reading and writing data,  PIN code must be send to danfoss.
I'm sending four zero bytes to charasteristic uuid_pin. You can try with nRF Connect.

2) All data is encrypted, so after reading , you should decrypt it.
And encrypt before writing. You can find encrypting and decrypting
functions in attached MS Visual Studio project.

3) Encryption key, which I struggled with much). You are able to read it,
only if you connect after pressing danfoss hard button.
It is used by encryption/decryption functions.

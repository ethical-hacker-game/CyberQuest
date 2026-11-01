{
  "command": "snmpwalk",
  "output": "USAGE: snmpwalk [OPTIONS] AGENT [OID]\n\nExample: snmpwalk -c brdgwks-ro 10.30.14.1",
  "errorSub": "Timeout: No Response from that agent, or the community string was rejected.",
  "subcommands": [
    {
      "subcommand": "-c brdgwks-ro 10.30.14.1",
      "output": "iso.3.6.1.2.1.1.1.0 = STRING: \"Halloway plant core stack, IOS 15.2(7)E4\"\niso.3.6.1.2.1.1.2.0 = OID: iso.3.6.1.4.1.9.1.1208\niso.3.6.1.2.1.1.3.0 = Timeticks: (631584000) 731 days, 0:00:00.00\niso.3.6.1.2.1.1.4.0 = STRING: \"BridgeWorks Automation NOC <noc@bridgeworksautomation.com>\"\niso.3.6.1.2.1.1.5.0 = STRING: \"HAL-CORE-SW\"\niso.3.6.1.2.1.1.6.0 = STRING: \"Halloway Precision - Plant 2 MDF\"\niso.3.6.1.2.1.1.7.0 = INTEGER: 78\n\nWalked 7 objects in the system group. Use the MIB Browser for the interface table."
    },
    {
      "subcommand": "-v 2c -c brdgwks-ro 10.30.14.1",
      "output": "iso.3.6.1.2.1.1.1.0 = STRING: \"Halloway plant core stack, IOS 15.2(7)E4\"\niso.3.6.1.2.1.1.4.0 = STRING: \"BridgeWorks Automation NOC <noc@bridgeworksautomation.com>\"\niso.3.6.1.2.1.1.5.0 = STRING: \"HAL-CORE-SW\"\niso.3.6.1.2.1.1.6.0 = STRING: \"Halloway Precision - Plant 2 MDF\""
    }
  ]
}

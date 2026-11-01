{
  "command": "ike-scan",
  "output": "ike-scan 1.9.5\nUsage: ike-scan [options] <host>\n\nExample: ike-scan 10.30.14.254",
  "errorSub": "ike-scan: no response from that host on UDP port 500.",
  "subcommands": [
    {
      "subcommand": "--aggressive 10.30.14.254",
      "output": "Starting ike-scan 1.9.5 with 1 hosts\n\n10.30.14.254  Aggressive Mode Handshake returned\n    HDR=(CKY-R=8f21c4a90b3e7712)\n    SA=(Enc=3DES Hash=SHA1 Auth=PSK Group=2:modp1024 LifeType=Seconds LifeDuration=28800)\n    ID=(ID_FQDN=bridgeworks-site)\n\nEnding ike-scan 1.9.5: 1 hosts scanned. 1 returned handshake."
    }
  ]
}

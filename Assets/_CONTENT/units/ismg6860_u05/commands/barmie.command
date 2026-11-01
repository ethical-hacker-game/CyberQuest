{
  "command": "barmie",
  "output": "BaRMIe v1.01 - Java RMI enumeration tool. Written by Nicky Bloor (@NickstaDB)\n\nUsage: barmie <target>\n\nBaRMIe must be used only against systems you have permission to test.",
  "errorSub": "BaRMIe: no RMI registry found at that address.",
  "subcommands": [
    {
      "subcommand": "10.30.14.52",
      "output": "Scanning 1 target(s) for objects exposed via an RMI registry...\n\nRMI Registry at 10.30.14.52:1099\nObjects exposed: 1\n\nObject 1\n  Name: HistorianFeed\n  Endpoint: 127.0.1.1:41855\n  Classes: 3\n    Classname: java.rmi.server.RemoteStub\n    Classname: java.rmi.server.RemoteObject\n    Classname: HistorianFeed_Stub\n\nSuccessfully scanned 1 target(s)."
    }
  ]
}

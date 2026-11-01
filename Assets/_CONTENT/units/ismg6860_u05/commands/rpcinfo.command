{
  "command": "rpcinfo",
  "output": "Usage: rpcinfo [-p host] [-T netid]\n\nExample: rpcinfo -p 10.30.14.52",
  "errorSub": "rpcinfo: can't contact portmapper at that address.",
  "subcommands": [
    {
      "subcommand": "-p 10.30.14.52",
      "output": "   program vers proto   port  service\n    100000    4   tcp    111  portmapper\n    100000    3   tcp    111  portmapper\n    100000    4   udp    111  portmapper\n    100005    1   udp  48122  mountd\n    100005    3   tcp  56031  mountd\n    100003    3   tcp   2049  nfs\n    100003    4   tcp   2049  nfs\n    100021    4   tcp  39114  nlockmgr"
    }
  ]
}

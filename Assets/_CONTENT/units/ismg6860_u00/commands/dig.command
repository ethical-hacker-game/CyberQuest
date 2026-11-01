{
  "command": "dig",
  "isTool": true,
  "output": "Usage: dig <domain>",
  "errorSub": "No DNS evidence exists for that name in the authorized case records. Try trainingnorthstar.com.",
  "subcommands": [
    {
      "subcommand": "trainingnorthstar.com",
      "output": "; <<>> DiG 9.18 <<>> trainingnorthstar.com\n;; QUESTION SECTION:\n;trainingnorthstar.com.        IN      A\n\n;; ANSWER SECTION:\ntrainingnorthstar.com. 300     IN      A       185.254.57.124\n\n;; AUTHORITY SECTION:\ntrainingnorthstar.com. 300     IN      NS      ns1.trainingnorthstar.com.\n\n;; Query time: 18 msec\n;; SERVER: academy-resolver.cyberquest.local#53\n;; WHEN: Wed Jul 08 11:00:00 2026\n;; MSG SIZE  rcvd: 94"
    }
  ]
}

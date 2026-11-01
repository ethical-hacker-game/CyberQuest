{
  "command": "dig",
  "output": "; <<>> DiG 9.16.1 <<>>\nUsage: dig [@server] [name] [type]\n\nExample: dig axfr hallowayprecision.com",
  "errorSub": "; Transfer failed. Check the zone name and try 'dig' for usage.",
  "subcommands": [
    {
      "subcommand": "axfr hallowayprecision.com",
      "output": "; <<>> DiG 9.16.1 <<>> @10.30.14.10 axfr hallowayprecision.com\n;; global options: +cmd\nhallowayprecision.com.   3600 IN SOA  ns1.hallowayprecision.com. hostmaster.hallowayprecision.com. 2026071801 3600 900 604800 86400\nhallowayprecision.com.   3600 IN NS   ns1.hallowayprecision.com.\nhallowayprecision.com.   3600 IN MX   10 mail.hallowayprecision.com.\nhallowayprecision.com.   3600 IN A    203.0.113.18\nns1.hallowayprecision.com. 3600 IN A  10.30.14.10\nmail.hallowayprecision.com. 3600 IN A 10.30.14.25\nwww.hallowayprecision.com. 3600 IN A  203.0.113.18\nvpn.hallowayprecision.com. 3600 IN A  203.0.113.20\nremote.hallowayprecision.com. 3600 IN A 198.51.100.77\nhallowayprecision.com.   3600 IN SOA  ns1.hallowayprecision.com. hostmaster.hallowayprecision.com. 2026071801 3600 900 604800 86400\n;; Query time: 6 msec\n;; XFR size: 10 records (messages 1, bytes 412)"
    },
    {
      "subcommand": "@10.30.14.10 axfr hallowayprecision.com",
      "output": "; <<>> DiG 9.16.1 <<>> @10.30.14.10 axfr hallowayprecision.com\n;; global options: +cmd\nhallowayprecision.com.   3600 IN SOA  ns1.hallowayprecision.com. hostmaster.hallowayprecision.com. 2026071801 3600 900 604800 86400\nhallowayprecision.com.   3600 IN NS   ns1.hallowayprecision.com.\nhallowayprecision.com.   3600 IN MX   10 mail.hallowayprecision.com.\nhallowayprecision.com.   3600 IN A    203.0.113.18\nns1.hallowayprecision.com. 3600 IN A  10.30.14.10\nmail.hallowayprecision.com. 3600 IN A 10.30.14.25\nwww.hallowayprecision.com. 3600 IN A  203.0.113.18\nvpn.hallowayprecision.com. 3600 IN A  203.0.113.20\nremote.hallowayprecision.com. 3600 IN A 198.51.100.77\nhallowayprecision.com.   3600 IN SOA  ns1.hallowayprecision.com. hostmaster.hallowayprecision.com. 2026071801 3600 900 604800 86400\n;; Query time: 6 msec\n;; XFR size: 10 records (messages 1, bytes 412)"
    },
    {
      "subcommand": "remote.hallowayprecision.com",
      "output": "; <<>> DiG 9.16.1 <<>> remote.hallowayprecision.com\n;; ANSWER SECTION:\nremote.hallowayprecision.com. 3600 IN A 198.51.100.77\n\n;; Query time: 4 msec"
    }
  ]
}

{
  "command": "dig",
  "isTool": false,
  "output": "Usage: dig [@server] [name] [type] [class] [options]\r\n\r\nExamples:\r\n  dig example.com\r\n  dig @8.8.8.8 example.com A\r\n\r\nFor more information on options, use: dig -h",
  "errorSub": "; <<>> Dig 9.10.6 <<>>\r\n;; global options: +cmd\r\n;; Got answer:\r\n;; ->>HEADER<<- opcode: QUERY, status: FORMERR, id: 67890\r\n;; flags: qr rd; QUERY: 0, ANSWER: 0, AUTHORITY: 0, ADDITIONAL: 0\r\n\r\n;; ERROR: malformed query or invalid input.\r\n;; Please check the domain name and try again.\r\n\r\n;; Query time: 5 msec\r\n;; SERVER: 192.0.2.53#53(192.0.2.53)\r\n;; WHEN: Fri Oct 13 13:00:00 2023\r\n;; MSG SIZE  rcvd: 50",
  "subcommands": [
    {
      "subcommand": "-h",
      "output": "Usage: dig [@server] [name] [type] [class] [options]\r\n\r\nOptions:\r\n  -h          Display this help message and exit.\r\n  @server     Specify a DNS server to query.\r\n  name        Domain name to query for.\r\n  type        Type of query: A, AAAA, MX, CNAME, TXT, etc.\r\n  class       Class of query (default IN).\r\n\r\nQuery Options:\r\n  +short      Provide a concise response.\r\n  +trace      Trace the delegation path to the name server.\r\n  +stats      Display statistics for the query.\r\n  +json       Output results in JSON format.\r\n\r\nExamples:\r\n  dig example.com\r\n  dig @8.8.8.8 example.com A\r\n  dig example.com MX +short\r\n\r\nFor further details, check the documentation."
    },
    {
      "subcommand": "NS BikeForgeGames.com",
      "output": "; <<>> Dig 9.10.6 <<>> NS BikeForgeGames.com\r\n;; global options: +cmd\r\n;; Got answer:\r\n;; ->>HEADER<<- opcode: QUERY, status: NOERROR, id: 22108\r\n;; flags: qr rd ra; QUERY: 1, ANSWER: 3, AUTHORITY: 0, ADDITIONAL: 3\r\n\r\n;; QUESTION SECTION:\r\n;BikeForgeGames.com.            IN      NS\r\n\r\n;; ANSWER SECTION:\r\nBikeForgeGames.com. 3600 IN NS ns1.bikeforge.host.\r\nBikeForgeGames.com. 3600 IN NS ns2.bikeforge.host.\r\nBikeForgeGames.com. 3600 IN NS duck451.net.\r\n\r\n;; ADDITIONAL SECTION:\r\nns1.bikeforge.host.    3600 IN A 198.51.100.24\r\nns2.bikeforge.host.    3600 IN A 198.51.100.25\r\nduck451.net.           3600 IN A 203.0.113.77\r\n\r\n;; Query time: 24 msec\r\n;; SERVER: 192.0.2.53#53(192.0.2.53)\r\n;; WHEN: Fri Oct 13 12:34:56 2023\r\n;; MSG SIZE  rcvd: 212"
    },
    {
      "subcommand": "BikeForgeGames.com",
      "output": "; <<>> Dig 9.10.6 <<>> BikeForgeGames.com\r\n;; global options: +cmd\r\n;; Got answer:\r\n;; ->>HEADER<<- opcode: QUERY, status: NOERROR, id: 45678\r\n;; flags: qr rd ra; QUERY: 1, ANSWER: 1, AUTHORITY: 3, ADDITIONAL: 3\r\n\r\n;; QUESTION SECTION:\r\n;BikeForgeGames.com.            IN      A\r\n\r\n;; ANSWER SECTION:\r\nBikeForgeGames.com. 3600 IN A 198.51.100.50\r\n\r\n;; AUTHORITY SECTION:\r\nBikeForgeGames.com. 3600 IN NS ns1.bikeforge.host.\r\nBikeForgeGames.com. 3600 IN NS ns2.bikeforge.host.\r\nBikeForgeGames.com. 3600 IN NS duck451.net.\r\n\r\n;; ADDITIONAL SECTION:\r\nns1.bikeforge.host.    3600 IN A 198.51.100.24\r\nns2.bikeforge.host.    3600 IN A 198.51.100.25\r\nduck451.net.           3600 IN A 203.0.113.77\r\n\r\n;; Query time: 18 msec\r\n;; SERVER: 192.0.2.53#53(192.0.2.53)\r\n;; WHEN: Fri Oct 13 12:45:23 2023\r\n;; MSG SIZE  rcvd: 268"
    },
    {
      "subcommand": "NS novasolutions.com",
      "output": ";; ->>HEADER<<- opcode: QUERY, status: NOERROR\n;; flags: qr aa; QUERY: 1, ANSWER: 1, AUTHORITY: 0\n;; QUESTION SECTION:\n;novasolutions.com.    IN    NS\n;; ANSWER SECTION:\nnovasolutions.com.    86400 IN NS ns1.novasolutions.com.\n;; SERVER: 198.51.100.30#53"
    },
    {
      "subcommand": "novasolutions.com NS",
      "output": ";; ->>HEADER<<- opcode: QUERY, status: NOERROR\n;; flags: qr aa; QUERY: 1, ANSWER: 1, AUTHORITY: 0\n;; QUESTION SECTION:\n;novasolutions.com.    IN    NS\n;; ANSWER SECTION:\nnovasolutions.com.    86400 IN NS ns1.novasolutions.com.\n;; SERVER: 198.51.100.30#53"
    },
    {
      "subcommand": "A novasolutions.com",
      "output": ";; ->>HEADER<<- opcode: QUERY, status: NOERROR\n;; flags: qr rd ra; QUERY: 1, ANSWER: 1, AUTHORITY: 0\n;; QUESTION SECTION:\n;novasolutions.com.    IN    A\n;; ANSWER SECTION:\nnovasolutions.com.    300 IN A 198.51.100.24\n;; SERVER: 192.0.2.53#53"
    },
    {
      "subcommand": "novasolutions.com A",
      "output": ";; ->>HEADER<<- opcode: QUERY, status: NOERROR\n;; flags: qr rd ra; QUERY: 1, ANSWER: 1, AUTHORITY: 0\n;; QUESTION SECTION:\n;novasolutions.com.    IN    A\n;; ANSWER SECTION:\nnovasolutions.com.    300 IN A 198.51.100.24\n;; SERVER: 192.0.2.53#53"
    }
  ]
}

{
  "command": "ldapsearch",
  "output": "usage: ldapsearch [options] [filter [attributes...]]\n\nExample: ldapsearch svc_bridgeworks\nExample: ldapsearch -b base",
  "errorSub": "ldap_search: No such object (32)",
  "subcommands": [
    {
      "subcommand": "svc_bridgeworks",
      "output": "ldapsearch -x -H ldap://10.30.14.10 -b \"DC=hallowayprecision,DC=com\" \"(sAMAccountName=svc_bridgeworks)\"\n\n# extended LDIF\n#\n# LDAPv3\n# base <DC=hallowayprecision,DC=com> with scope subtree\n# filter: (sAMAccountName=svc_bridgeworks)\n#\n\n# svc_bridgeworks, Service Accounts, BridgeWorks, hallowayprecision.com\ndn: CN=svc_bridgeworks,OU=Service Accounts,OU=BridgeWorks,DC=hallowayprecision,DC=com\nobjectClass: user\nsAMAccountName: svc_bridgeworks\ndisplayName: BridgeWorks Historian Service\ndescription: BridgeWorks Automation - historian service integration\nuserAccountControl: 66048 (NORMAL_ACCOUNT | DONT_EXPIRE_PASSWORD)\nlockoutTime: 0\nlastLogonTimestamp: 2026-07-24 02:41:17\nobjectSid: S-1-5-21-1844237615-3092331701-2044718958-1219\n\n# search result\nsearch: 2\nresult: 0 Success\n\n# numResponses: 2\n# numEntries: 1"
    },
    {
      "subcommand": "-b base",
      "output": "ldapsearch -x -H ldap://10.30.14.10 -b \"DC=hallowayprecision,DC=com\" \"(objectClass=organizationalUnit)\"\n\ndn: OU=Engineering,DC=hallowayprecision,DC=com\ndn: OU=Plant Operations,DC=hallowayprecision,DC=com\ndn: OU=Quality,DC=hallowayprecision,DC=com\ndn: OU=Service Accounts,DC=hallowayprecision,DC=com\ndn: OU=BridgeWorks,DC=hallowayprecision,DC=com\n\n# numEntries: 5\n\nAnonymous bind accepted."
    }
  ]
}

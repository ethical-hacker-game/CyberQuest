{
  "command": "nikto",
  "output": "Usage: nikto -h <target>",
  "errorSub": "No Nikto results exist for that target in this evidence package.",
  "requiredSkillID": "niktoscan",
  "subcommands": [
    {
      "subcommand": "-h https://portal.northbridge.com",
      "output": "Nikto scan summary for https://portal.northbridge.com\n+ Server: Apache/2.4.49\n+ Reported HTTP methods: GET, HEAD, POST, TRACE\n+ X-Frame-Options: not present in the observed response\n+ /setup/install/ returned HTTP 200\n+ /public/backups/ returned a directory index\n\nSupplementary evidence supplied by Northbridge (not collected by Nikto):\nAccess-log excerpt: repeated probes of /setup/ and /public/backups/ from external host 198.51.100.23 over the past week; activity predates this engagement."
    },
    {
      "subcommand": "-h https://portal.northbridge.com/setup",
      "output": "Nikto setup route check\n+ /setup/install/ returned HTTP 200\n+ Page title: Northbridge Portal Setup Wizard\n+ Recommendation: remove setup routes after deployment or restrict them to approved maintenance hosts."
    },
    {
      "subcommand": "-h https://portal.northbridge.com/public/backups/",
      "output": "Nikto directory listing check\n+ Directory indexing enabled at /public/backups/\n+ Interesting file: admin-backup.zip\n+ admin-backup.zip Last-Modified: 2026-05-28 (weeks after the setup wizard it references was scheduled for decommission: the archive was touched well after the component should have been removed)\n+ Recommendation: disable indexing and move backup archives outside public web roots."
    }
  ]
}

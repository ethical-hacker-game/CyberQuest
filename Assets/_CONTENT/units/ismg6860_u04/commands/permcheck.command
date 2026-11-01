{
  "command": "permcheck",
  "output": "Usage: permcheck <evidence-package>",
  "errorSub": "Unknown evidence package.",
  "subcommands": [
    {
      "subcommand": "northbridge-docs",
      "output": "Northbridge Document Exchange - supplied permission listing\nPaths below are local server paths, not website URLs.\n\n/etc/northbridge/web.config\n  purpose=private service settings\n  owner=svc-web group=webops mode=rw-r--r--\n/var/northbridge/public-notice.txt\n  purpose=public service notice\n  owner=svc-web group=webops mode=rw-r--r--\n/var/log/northbridge/service.log\n  purpose=internal service log\n  owner=svc-web group=webops mode=rw-r-----\n/etc/northbridge/backup.conf\n  purpose=private backup settings\n  owner=root group=backupops mode=rw-------\n\n4 file records. File contents and access logs are not included."
    }
  ]
}

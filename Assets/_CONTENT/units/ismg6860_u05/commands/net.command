{
  "command": "net",
  "output": "The syntax of this command is:\n\nNET [ VIEW | USE | STATISTICS ]",
  "errorSub": "The service name is invalid. Try 'net view' or 'net statistics workstation'.",
  "subcommands": [
    {
      "subcommand": "view \\\\FORGE-HIST01",
      "output": "Shared resources at \\\\FORGE-HIST01\n\nShare name     Type   Used as  Comment\n-------------------------------------------------------------------------------\nhistorian      Disk            Process historian archive\nprint$         Disk            Printer Drivers\nThe command completed successfully.\n\nNote: hidden shares ending in $ are not listed by net view."
    },
    {
      "subcommand": "statistics workstation",
      "output": "Workstation Statistics for \\\\HAL-ENG-04\n\nStatistics since 2026-07-20 06:00:11\n\n  Bytes received                            41908224\n  Server Message Blocks (SMBs) received     10442\n  Bytes transmitted                          8811520\n  Sessions started                              14\n  Failed sessions                                0\n\nThe command completed successfully."
    }
  ]
}

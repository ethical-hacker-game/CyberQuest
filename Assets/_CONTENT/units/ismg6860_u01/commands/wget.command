{
  "command": "wget",
  "output": "add parameters",
  "errorSub": "parameters are incorrect",
  "subcommands": [
    {
      "subcommand": "--mirror -p --convert-links -P ./localdir http://north-tec.com",
      "output": "--2024-09-29 15:00:00--  http://north-tec.com/\r\nResolving north-tec.com (north-tec.com)... 203.0.113.12, 203.0.113.13\r\nConnecting to north-tec.com (north-tec.com)|203.0.113.12|:80... connected.\r\nHTTP request sent, awaiting response... 200 OK\r\nLength: unspecified [text/html]\r\nSaving to: './localdir/north-tec.com/index.html'\r\n\r\nnorth-tec.com/index.html     [ <=>                  ]  16.43K  --.-KB/s    in 0.1s    \r\n\r\n2024-09-29 15:00:01 (150 KB/s) - './localdir/north-tec.com/index.html' saved [16832]\r\n\r\n--2024-09-29 15:00:01--  http://north-tec.com/assets/logo.png\r\nReusing existing connection to north-tec.com:80.\r\nHTTP request sent, awaiting response... 200 OK\r\nLength: 4.5K [image/png]\r\nSaving to: './localdir/north-tec.com/assets/logo.png'\r\n\r\nnorth-tec.com/assets/logo.png [100%]    4.5K  --.-KB/s    in 0.02s   \r\n\r\n2024-09-29 15:00:02 (190 KB/s) - './localdir/north-tec.com/assets/logo.png' saved [4512/4512]\r\n\r\n--2024-09-29 15:00:02--  http://north-tec.com/styles.css\r\nReusing existing connection to north-tec.com:80.\r\nHTTP request sent, awaiting response... 200 OK\r\nLength: 1.2K [text/css]\r\nSaving to: './localdir/north-tec.com/styles.css'\r\n\r\nnorth-tec.com/styles.css  [100%]   1.2K  --.-KB/s    in 0.01s    \r\n\r\n2024-09-29 15:00:03 (120 KB/s) - './localdir/north-tec.com/styles.css' saved [1234/1234]\r\n\r\nFINISHED --2024-09-29 15:00:03--\r\nTotal wall clock time: 3s\r\nDownloaded: 3 files, 22K in 0.14s (160 KB/s)\r"
    }
  ]
}
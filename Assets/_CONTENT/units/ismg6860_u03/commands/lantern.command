{
  "command": "lantern",
  "isTool": true,
  "output": "LanternPass incident evidence helper. Use the provided subcommands.",
  "errorSub": "Unknown Lantern evidence command. Try preview sms, carrier simswap, decode qr, bluetooth share, app compare, dns compare, or final indicators.",
  "subcommands": [
    {
      "subcommand": "preview sms",
      "output": "SMS preview\nDisplayed sender: HV-Badge\nExpanded URL: https://lanternpass-alerts.com/alerts/revalidate.html\nShort link source: hv-lantern.link/r/BL-LANTERN-9"
    },
    {
      "subcommand": "carrier simswap",
      "output": "Carrier fraud note\nTarget number: Riley Cho\nRecovery attempt: 07:42\nIndicator: SIM swap request with failed PIN reset\nCampaign marker: BL-LANTERN-9"
    },
    {
      "subcommand": "decode qr",
      "output": "QR decode result\nDecoded URL: https://harbor-badge-verify.com/revalidate\nSticker location: West Gate registration desk\nCampaign marker: BL-LANTERN-9"
    },
    {
      "subcommand": "bluetooth share",
      "output": "Proximity share log\nDevice name: Lantern Support Kiosk\nFile offered: Expo_FloorPlan_Update.mobileconfig\nRisk: unsolicited configuration profile\nCampaign marker: BL-LANTERN-9"
    },
    {
      "subcommand": "app compare",
      "output": "LanternPass package comparison\nOfficial package: com.harborview.lanternpass\nSuspicious package: com.bluelantern.lanternpass\nSuspicious hash prefix: b7f4-lantern\nPublisher mismatch: Blue Lantern Mobile"
    },
    {
      "subcommand": "dns compare",
      "output": "DNS comparison\nExpected lanternpass-alerts.com -> 203.0.113.44\nObserved affected clients -> 198.51.100.73\nFinding: familiar URL routed to a fake page during the pharming window."
    },
    {
      "subcommand": "final indicators",
      "output": "Blue Lantern indicators\nMarker: BL-LANTERN-9\nPrimary channels: SMS, voice call, cloned social profile, QR sticker, mobile app, calendar invite, group chat, fake VPN, DNS redirection\nRecommended action: preserve evidence, warn staff with specific indicators, and keep credential collection out of training simulations."
    }
  ]
}

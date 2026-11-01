{
  "command": "languard",
  "output": "Usage: languard <audit endpoints | audit services | audit scheduled-tasks | policy passwords>",
  "errorSub": "Unknown GFI LanGuard evidence request.",
  "subcommands": [
    {
      "subcommand": "audit endpoints",
      "output": "GFI LanGuard Endpoint Audit (overview)\nHost NB-FIN-07 severity=High headline=missing critical patches\nHost NB-HR-02 severity=Medium headline=browser patch pending\nHost NB-OPS-11 severity=Low headline=local audit policy incomplete\nRecommended first action: patch NB-FIN-07.\nTip: run 'languard audit services', 'languard policy passwords', or 'languard audit scheduled-tasks' for the NB-FIN-07 details."
    },
    {
      "subcommand": "audit services",
      "output": "GFI LanGuard Service Permission Review: NB-FIN-07\nFinding: weak service permissions on a local service (writable by non-admin accounts)\nImpact: a low-privilege foothold could modify the service and gain higher privileges."
    },
    {
      "subcommand": "policy passwords",
      "output": "GFI LanGuard Credential Policy Review: NB-FIN-07\nPassword policy: minimum length 8\nAccount lockout: not configured (no lockout)\nImpact: short passwords with no lockout lower the cost of password guessing and cracking."
    },
    {
      "subcommand": "audit scheduled-tasks",
      "output": "GFI LanGuard Scheduled Task Review: NB-FIN-07\nScheduled task 'NightlyExport' runs as svc-web (overprivileged service account)\nImpact: an overprivileged scheduled task can help unauthorized access persist after an initial compromise."
    }
  ]
}

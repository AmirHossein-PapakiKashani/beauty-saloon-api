# Learning: one use-case ≠ full backend

After manual Cline Inject, GetAvailableDays shipped with green tests (13/13). Amir asked if "all infrastructure" is done.

**Insight:** That run only delivered (1) one calendar read endpoint and (2) thin host bootstrap (`ApiResponse`, DI, controllers). It did **not** deliver EF/Postgres, Auth OTP, SalonServices, Staff, Appointments, or the other 16 queue items. Queue `completed: 17` remains a false positive from stub Inject + placeholder tests earlier.

**Next teaching step:** reset queue (keep only `get-available-days` completed), then run `GetActiveSalonServices` as the real foundation slice.

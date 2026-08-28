# 12: Integration Testing

**What to build:** Viết test cases để verify end-to-end flow của health check system.

**Blocked by:** 10 (FrmMain UI), 11 (FrmDeviceMonitor)

**Status:** ready-for-agent

## Acceptance criteria

- [ ] Test: PingFail → Retry → Error state → LiveView stopped
- [ ] Test: PingSuccess after offline → Auto reconnect → LiveView started
- [ ] Test: Manual restart button → SDK reconnect → State restored
- [ ] Test: Multiple devices offline → Only affected panels show error
- [ ] Test: Controller offline → ReadLog stopped
- [ ] Test: All devices online on startup → Streams show

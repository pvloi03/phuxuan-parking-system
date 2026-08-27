# Domain Docs

**Layout:** Single-context

---

## Consumer Rules

When working in this codebase, agents should:

1. **Read `CONTEXT.md`** at the repo root for:
   - Domain language glossary (Entity names, Value Objects, Ubiquitous terms)
   - Business rules and invariants
   - Open questions and decisions

2. **Read `docs/DOCS.md`** for:
   - Technical architecture documentation
   - API endpoints and patterns
   - Device integration (ZKTeco, Hikvision, NST cameras)
   - WinForms UI structure

3. **Check `docs/tasks/`** for:
   - Task specifications and progress
   - Implementation checklists

---

## File Locations

| File | Purpose |
|------|---------|
| `CONTEXT.md` | Domain glossary, business rules |
| `docs/DOCS.md` | Technical documentation |
| `docs/tasks/` | Task specifications |
| `docs/tai-lieu-kiem-thu-test-cases.md` | Test cases |
| `docs/thiet-ke-kien-truc-he-thong-kiem-soat-xe.md` | Architecture design |

---

## Context Pointers

| Pointer | Trigger | Target |
|---------|---------|--------|
| **domain-model** | Entity, Value Object, Enum, business rule | `CONTEXT.md` |
| **hardware-layer** | SDK, P/Invoke, Camera, Controller, ZKTeco, Hikvision, NST | `docs/DOCS.md` §2-3 |
| **web-stack** | API Controller, React page, JWT, RBAC | `docs/DOCS.md` §8-9 |
| **winforms-stack** | FrmMain, FrmDeviceMonitor, WinForms UI | `docs/DOCS.md` §5-7 |

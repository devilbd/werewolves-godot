# .agents/ Directory Index

This directory contains specialized role instructions, task playbooks, and development workflows for AI agents operating in the **Werewolves (Godot 4 C# Edition)** repository.

---

## 📂 Directory Map

```text
.agents/
├── README.md                  # This overview and role directory
├── roles/                     # Domain-specific persona guidelines
│   ├── gameplay-engineer.md   # Mechanics, combat, state machine, entity behaviors
│   ├── ui-ux-engineer.md      # HUD, Diablo orbs, CanvasLayer, responsive layouts, pouch UI
│   ├── world-architect.md     # Procedural generation, grid maps, biomes, collision
│   └── qa-engineer.md         # Build verification, unit/smoke testing, save file testing
└── workflows/                 # Actionable, step-by-step feature playbooks
    ├── add-skill.md           # Guide to adding new werewolf combat skills
    ├── add-creature.md        # Guide to creating wildlife or enemy AI entities
    └── add-item.md            # Guide to implementing collectible items & pouch icons
```

---

## 🎯 How to Use These Agent Guidelines

- **When assuming a specific task**: Review the corresponding role document in `roles/` to adopt domain-specific constraints, best practices, and code patterns.
- **When implementing a new feature**: Follow the relevant step-by-step checklist in `workflows/` to ensure full integration across all required subsystems (scripts, inputs, scenes, and serialization).
- **Core Architecture Reference**: See [GEMINI.md](../GEMINI.md) for full project architecture and C# conventions.
- **General SOPs**: See [AGENTS.md](../AGENTS.md) for agent collaboration rules and safety standards.

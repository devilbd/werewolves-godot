# Workflow: Adding a New Werewolf Skill

This playbook outlines the exact step-by-step procedure for introducing a new combat ability into the game.

---

## 📋 Checklist

- [ ] **Step 1**: Register Input Action in `project.godot`
- [ ] **Step 2**: Update Cooldown & State Arrays in `GameState.cs`
- [ ] **Step 3**: Define Damage / Effect Formula in `Formulas.cs`
- [ ] **Step 4**: Implement Execution Logic & Animation in `Werewolf.cs`
- [ ] **Step 5**: Update UI Hotkey & Icon Slot in `StatsPanel.cs`
- [ ] **Step 6**: Verify Build & Compilation

---

## Detailed Steps

### Step 1: Register Input Action
In `project.godot`, locate the `[input]` section and declare the new hotkey:
```ini
skill_5={
"deadzone": 0.5,
"events": [Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":0,"physical_keycode":53,"key_label":0,"unicode":53,"location":0,"echo":false,"script":null)
]
}
```

### Step 2: Update Cooldowns in `GameState.cs`
Expand the cooldown array sizes and assign default duration:
```csharp
// Change array size from 4 to 5
public float[] SkillCooldownRemaining { get; } = new float[5];
public float[] SkillCooldownTotal { get; } = new float[] { 2.0f, 2.0f, 2.0f, 60.0f, 15.0f };
```

### Step 3: Add Formula in `Formulas.cs`
Add the mathematical formula for the skill:
```csharp
public static float CalculateSkill5Damage(ICombatant dealer, ICombatant target)
{
    float damageDealt = dealer.BaseDamage * 1.5f - target.BaseDefense / 2.0f + 25.0f;
    return Math.Max(0f, damageDealt);
}
```

### Step 4: Implement Execution in `Werewolf.cs`
1. Hook input in `HandleSkillInputs()`:
   ```csharp
   else if (Input.IsActionJustPressed("skill_5")) UseSkill(4);
   ```
2. Implement case in `UseSkill(int skillIndex)`:
   ```csharp
   case 4: // New Skill Name
       if (GameState.Instance.PlayerPower >= 30f && GameState.Instance.SelectedTarget is ICombatant target && !target.IsDead)
       {
           GameState.Instance.ModifyPower(-30f);
           GameState.Instance.StartSkillCooldown(4);
           TriggerAttackAnimation(1);

           if (Formulas.IsHitSuccessful(this, target))
           {
               float dmg = Formulas.CalculateSkill5Damage(this, target);
               target.TakeDamage(dmg, isSkill: true);
               GameState.Instance.TriggerDamageNumber(Mathf.FloorToInt(dmg).ToString(), target.GlobalPosition + new Vector2(0, -50), new Color(1f, 0.4f, 0.1f));
           }
       }
       break;
   ```

### Step 5: Update UI in `StatsPanel.cs`
1. Add skill icon and power cost:
   ```csharp
   private readonly string[] _iconPaths = new[]
   {
       "res://assets/icons/scratch_hit_icon.png",
       "res://assets/icons/charge_attack.png",
       "res://assets/icons/bite.png",
       "res://assets/icons/blood_howling.png",
       "res://assets/icons/new_skill_icon.png"
   };

   private readonly float[] _powerCosts = new[] { 15f, 25f, 20f, 40f, 30f };
   ```
2. Update the slot generation loops to iterate through `_iconPaths.Length` instead of hardcoded `4`.

### Step 6: Verify Build
Execute:
```bash
dotnet build Werewolves.sln
```
Ensure 0 warnings and 0 errors.

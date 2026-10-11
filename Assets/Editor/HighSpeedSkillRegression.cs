using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;

public static class HighSpeedSkillRegression
{
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static long now;
    static int checks;
    static void Assert(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    static void Set(Type type, object target, string name, object value) { type.GetField(name, Flags).SetValue(target, value); }
    static object Get(Type type, object target, string name) { return type.GetField(name, Flags).GetValue(target); }
    static object Call(object target, string name, params object[] args)
    {
        var method = target.GetType().GetMethod(name, Flags);
        var parameters = method.GetParameters();
        if (args.Length < parameters.Length)
        {
            var complete = new object[parameters.Length]; Array.Copy(args, complete, args.Length);
            for (int i = args.Length; i < complete.Length; i++) complete[i] = parameters[i].DefaultValue;
            args = complete;
        }
        return method.Invoke(target, args);
    }

    public class Sink1 : Game1.ISession { public List<sbyte[]> Packets = new List<sbyte[]>(); public void sendMessage(Game1.Message m) { Packets.Add(m.getData()); } }
    public class Sink2 : Game2.ISession { public List<sbyte[]> Packets = new List<sbyte[]>(); public void sendMessage(Game2.Message m) { Packets.Add(m.getData()); } }

    sealed class Fixture
    {
        public string Namespace;
        public Type CharacterType, ScreenType;
        public object Actor, Skill, Sink;
        public List<sbyte[]> Packets;
        public Type Type(string name) { return typeof(Game1.Char).Assembly.GetType(Namespace + "." + name, true); }
        public Fixture(string ns, int template, bool local)
        {
            Namespace = ns;
            now = 1000000;
            CharacterType = Type("Char"); ScreenType = Type("GameScr");
            Actor = Activator.CreateInstance(CharacterType);
            Set(CharacterType, null, "myChar", Actor);
            object screen = FormatterServices.GetUninitializedObject(ScreenType);
            Set(ScreenType, null, "instance", screen);
            Set(Type("GameCanvas"), null, "debugSession", Activator.CreateInstance(Type("MyVector")));
            Set(ScreenType, screen, "isSuperPower", true); // Disable audio-only explosion setup.
            Set(Type("SoundMn"), null, "isSound", false);
            Set(Type("Sound"), null, "player", new GameObject[256]);
            Set(Type("Sound"), null, "music", new AudioClip[256]);
            var effects = Array.CreateInstance(Type("EffectCharPaint"), 200);
            for (int i = 0; i < effects.Length; i++)
            {
                var effect = Activator.CreateInstance(Type("EffectCharPaint"));
                var frames = Array.CreateInstance(Type("EffectInfoPaint"), 1);
                frames.SetValue(Activator.CreateInstance(Type("EffectInfoPaint")), 0);
                Set(effect.GetType(), effect, "arrEfInfo", frames); effects.SetValue(effect, i);
            }
            Set(ScreenType, null, "efs", effects);
            var paints = Array.CreateInstance(Type("SkillPaint"), 200);
            for (int i = 0; i < paints.Length; i++)
            {
                object paint = Activator.CreateInstance(Type("SkillPaint"));
                Set(paint.GetType(), paint, "id", 100);
                Set(paint.GetType(), paint, "effectHappenOnMob", 1);
                paints.SetValue(paint, i);
            }
            Set(ScreenType, null, "sks", paints);
            Type tiles = Type("TileMap");
            Set(tiles, null, "mapID", (short)10);
            Set(tiles, null, "tmw", 32); Set(tiles, null, "tmh", 32);
            var types = new int[1024]; for (int i = 0; i < types.Length; i++) types[i] = 2;
            Set(tiles, null, "types", types);
            Set(CharacterType, Actor, "me", local); Set(CharacterType, Actor, "statusMe", 1);
            Set(CharacterType, Actor, "charID", 1); Set(CharacterType, Actor, "cHP", 100L);
            Set(CharacterType, Actor, "cHPFull", 100L); Set(CharacterType, Actor, "cMP", 100L);
            Set(CharacterType, Actor, "cMPFull", 100L); Set(CharacterType, Actor, "cStamina", (short)20);
            Set(CharacterType, Actor, "cx", 240); Set(CharacterType, Actor, "cy", 240);
            Set(CharacterType, Actor, "cgender", template == 10 ? 0 : template == 11 ? 1 : 2);
            Skill = Activator.CreateInstance(Type("Skill"));
            object skillTemplate = Activator.CreateInstance(Type("SkillTemplate"));
            Set(skillTemplate.GetType(), skillTemplate, "id", (sbyte)template);
            Set(skillTemplate.GetType(), skillTemplate, "type", template == 13 || template == 14 ? 3 : 1);
            Set(Skill.GetType(), Skill, "template", skillTemplate);
            Set(Skill.GetType(), Skill, "skillId", (short)1); Set(Skill.GetType(), Skill, "manaUse", 10);
            Set(Skill.GetType(), Skill, "coolDown", 10000);
            Set(CharacterType, Actor, "myskill", Skill); Set(CharacterType, Actor, "skillTemplateId", 1);
            object mob = FormatterServices.GetUninitializedObject(Type("Mob"));
            Set(mob.GetType(), mob, "status", 2); Set(CharacterType, Actor, "mobFocus", mob);
            object mobs = Activator.CreateInstance(Type("MyVector")); Call(mobs, "addElement", mob);
            Set(ScreenType, null, "vMob", mobs);
            Set(ScreenType, null, "vCharInMap", Activator.CreateInstance(Type("MyVector")));
            object service = Type("Service").GetMethod("gI").Invoke(null, null);
            if (ns == "Game1") { var sink = new Sink1(); Sink = sink; Packets = sink.Packets; }
            else { var sink = new Sink2(); Sink = sink; Packets = sink.Packets; }
            Set(service.GetType(), service, "session", Sink);
        }
        public bool Flag(string name) { return (bool)Get(CharacterType, Actor, name); }
        public void Tick() { Call(Actor, "updateChargeSkill"); }
        public int EffectCount() { object v = Get(Type("Effect2"), null, "vEffect2"); return (int)Call(v, "size"); }
        public void ResetEffects() { Call(Get(Type("Effect2"), null, "vEffect2"), "removeAllElements"); }
        public void Receive(int command, sbyte[] data)
        {
            object packet = Activator.CreateInstance(Type("Message"), new object[] { (sbyte)command, data });
            object controller = Type("Controller").GetMethod("gI").Invoke(null, null);
            Call(controller, "onMessage", packet);
        }
    }

    public static void Run()
    {
        var clock = SkillCastTiming.NowMilliseconds;
        SkillCastTiming.NowMilliseconds = () => now;
        try
        {
            foreach (string ns in new[] { "Game1", "Game2" })
            {
                ExistingEffectsContinue(ns);
                CancelAfterDeadline(ns); DeathLock(ns); TargetDiesDuringRelease(ns);
                foreach (int speed in new[] { 1, 2, 5, 10, 20, 50, 100 })
                    foreach (int template in new[] { 14, 10, 11 }) Cast(ns, template, speed);
                Monkey(ns); Cleanup(ns); Cooldown(ns); DuplicateAndDelayedPrepare(ns);
                Debug.Log("PASS " + ns + " skill integration: x1..x100, release packets, mana, cooldown, monkey, cancellation, duplicate prepare");
            }
            Debug.Log("HIGH_SPEED_SKILL_REGRESSION_PASS checks=" + checks);
        }
        finally { SkillCastTiming.NowMilliseconds = clock; }
    }

    public static void RunReviewRegressions()
    {
        var clock = SkillCastTiming.NowMilliseconds;
        SkillCastTiming.NowMilliseconds = () => now;
        int failures = 0;
        try
        {
            foreach (string ns in new[] { "Game1", "Game2" })
                foreach (Action<string> test in new Action<string>[] { CancelAfterDeadline, DeathLock, TargetDiesDuringRelease })
                    try { test(ns); }
                    catch (Exception ex) { failures++; Debug.LogError("REVIEW_RED " + ns + " " + ex); }
            if (failures != 0) throw new Exception("REVIEW_REGRESSION_FAILURES=" + failures);
            Debug.Log("REVIEW_REGRESSIONS_PASS");
        }
        finally { SkillCastTiming.NowMilliseconds = clock; }
    }

    static void CancelAfterDeadline(string ns)
    {
        var f = new Fixture(ns, 10, true); Call(f.Actor, "sendUseChargeSkill"); Call(f.Actor, "ApplyChargeDuration", 4000);
        now += 4000; f.Tick(); Call(f.Actor, "stopUseChargeSkill"); Call(f.Actor, "setAttack");
        Assert(f.Packets.Count == 1, ns + " cancelled cast must block a late projectile attack callback");
    }

    static void DeathLock(string ns)
    {
        var f = new Fixture(ns, 10, true); Call(f.Actor, "sendUseChargeSkill"); Call(f.Actor, "ApplyChargeDuration", 4000);
        f.Tick(); Call(f.Actor, "startDie", (short)240, (short)240);
        Assert(f.Flag("isLockMove"), ns + " death must keep movement locked after cast cleanup");
        Call(f.Actor, "liveFromDead");
        Assert(!f.Flag("isLockMove"), ns + " resurrection must release movement lock");
    }

    static void TargetDiesDuringRelease(string ns)
    {
        var f = new Fixture(ns, 10, true); Call(f.Actor, "sendUseChargeSkill"); Call(f.Actor, "ApplyChargeDuration", 4000);
        now += 4000; f.Tick();
        object mob = Get(f.CharacterType, f.Actor, "mobFocus"); Set(mob.GetType(), mob, "status", 0);
        Call(f.Actor, "updateSkillPaint");
        Assert(!f.Flag("isUseSkillAfterCharge") && !f.Flag("isCreateDark"), ns + " target dying during release must clear cast immediately");
        Assert(f.Packets.Count == 1, ns + " dead target must not receive release");
    }

    static void ExistingEffectsContinue(string ns)
    {
        var f = new Fixture(ns, 10, true); Call(f.Actor, "sendUseChargeSkill"); Call(f.Actor, "ApplyChargeDuration", 4000);
        object effect = Activator.CreateInstance(f.Type("EffectPaint"));
        object paint = Activator.CreateInstance(f.Type("EffectCharPaint"));
        var frames = Array.CreateInstance(f.Type("EffectInfoPaint"), 2);
        Set(paint.GetType(), paint, "arrEfInfo", frames);
        Set(effect.GetType(), effect, "effCharPaint", paint);
        var effects = Array.CreateInstance(f.Type("EffectPaint"), 1); effects.SetValue(effect, 0);
        Set(f.CharacterType, f.Actor, "effPaints", effects);
        f.Tick();
        Assert((int)Get(effect.GetType(), effect, "index") == 1, ns + " existing character effects must keep advancing during charge");
        Call(f.Actor, "stopUseChargeSkill");
    }

    static void Cast(string ns, int template, int speed)
    {
        var f = new Fixture(ns, template, true);
        f.ResetEffects();
        if (template == 14) Call(f.Actor, "useSkillNotFocus"); else Call(f.Actor, "sendUseChargeSkill");
        int duration = template == 14 ? 2000 : template == 10 ? 4000 : 3000;
        Call(f.Actor, "ApplyChargeDuration", duration);
        int prepareCount = f.Packets.Count;
        Assert(prepareCount == 1, ns + " exactly one prepare");
        long initial = now;
        int ticks = duration * 50 * speed / 1000;
        for (int i = 0; i < ticks; i++)
        {
            now = initial + (long)i * duration / ticks; f.Tick();
            Assert(f.Flag(template == 10 ? "isFlyAndCharge" : "isStandAndCharge"), ns + " premature cancellation template=" + template + " speed=" + speed);
        }
        Assert(f.Packets.Count == prepareCount, ns + " no early release");
        Assert(f.EffectCount() <= 2 * (duration / 100 + 1), ns + " bounded effects at speed=" + speed);
        now = initial + duration; f.Tick();
        Assert(!f.Flag("isStandAndCharge") && !f.Flag("isFlyAndCharge"), ns + " completes on deadline");
        if (template != 14) { Call(f.Actor, "setAttack"); Call(f.Actor, "setAttack"); }
        Assert(f.Packets.Count == prepareCount + 1, ns + " exactly one release");
        Assert((long)Get(f.CharacterType, f.Actor, "cMP") == 90L, ns + " mana debited once");
        Assert((long)Get(f.Skill.GetType(), f.Skill, "lastTimeUseThisSkill") > 0L, ns + " cooldown starts");
        Call(f.Actor, "stopUseChargeSkill");
        Assert(!f.Flag("isLockMove"), ns + " releases movement lock");
    }

    static void Monkey(string ns)
    {
        var f = new Fixture(ns, 13, true); Call(f.Actor, "useSkillNotFocus");
        f.Tick(); Assert(f.Flag("isWaitMonkey"), ns + " first monkey waits");
        now += 1500; f.Tick(); Assert(f.Flag("isWaitMonkey"), ns + " monkey waits for authoritative transform");
        f.Receive(-90, new sbyte[] { 0, 0, 0, 0, 1, -1, -1, -1, -1, -1, -1, 1 });
        Assert(!f.Flag("isWaitMonkey") && !f.Flag("isLockMove"), ns + " transform clears cast lock");
        Assert((sbyte)Get(f.CharacterType, f.Actor, "isMonkey") == 1, ns + " transform applied");
    }

    static void Cleanup(string ns)
    {
        foreach (string cause in new[] { "death", "map", "timeout", "target" })
        {
            var f = new Fixture(ns, 10, true); Call(f.Actor, "sendUseChargeSkill"); Call(f.Actor, "ApplyChargeDuration", 4000);
            f.Tick();
            if (cause == "death") Set(f.CharacterType, f.Actor, "statusMe", 5);
            else if (cause == "map") Set(f.Type("TileMap"), null, "mapID", (short)11);
            else if (cause == "timeout") now += 14000;
            else { Set(Get(f.CharacterType, f.Actor, "mobFocus").GetType(), Get(f.CharacterType, f.Actor, "mobFocus"), "status", 0); now += 4000; }
            f.Tick();
            Assert(!f.Flag("isFlyAndCharge") && !f.Flag("isStandAndCharge"), ns + " cleanup " + cause);
            Assert(f.Packets.Count == 1, ns + " no release after " + cause);
        }
    }

    static void Cooldown(string ns)
    {
        var f = new Fixture(ns, 10, true);
        object vector = Activator.CreateInstance(f.Type("MyVector")); Call(vector, "addElement", f.Skill); Set(f.CharacterType, f.Actor, "vSkill", vector);
        f.Receive(-94, new sbyte[] { 0, 1, 0, 0, 39, 16 });
        long full = (long)Get(f.Skill.GetType(), f.Skill, "lastTimeUseThisSkill");
        Assert(full > 0, ns + " accepts full cooldown");
        f.Receive(-94, new sbyte[] { 0, 1, -1, -1, -1, -1 });
        Assert((long)Get(f.Skill.GetType(), f.Skill, "lastTimeUseThisSkill") == full, ns + " rejects negative cooldown");
        f.Receive(-94, new sbyte[] { 0, 1, 0, 0, 0, 0 });
        Assert((long)Get(f.Skill.GetType(), f.Skill, "lastTimeUseThisSkill") <= full - 9990, ns + " accepts cooldown zero");
    }

    static void DuplicateAndDelayedPrepare(string ns)
    {
        var f = new Fixture(ns, 10, true); Call(f.Actor, "sendUseChargeSkill"); now += 500; Call(f.Actor, "ApplyChargeDuration", 4000);
        long deadline = (long)Get(f.CharacterType, f.Actor, "chargeDeadline");
        now += 500; Call(f.Actor, "ApplyChargeDuration", 4000);
        Assert((long)Get(f.CharacterType, f.Actor, "chargeDeadline") == deadline, ns + " duplicate prepare cannot extend deadline");
        for (int i = 0; i < 20; i++) f.Tick();
        Assert((long)Get(f.CharacterType, f.Actor, "chargeDeadline") == deadline, ns + " ascent cannot restart deadline");
        now = deadline - 1; f.Tick(); Assert(f.Flag("isFlyAndCharge"), ns + " full delayed duration retained");
        Call(f.Actor, "stopUseChargeSkill");
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Hw9
{
    // ЗАДАЧА 1. Система эффектов состояния
    public interface IStatusEffect
    {
        string Name { get; }
        int RemainingTurns { get; }
        bool IsStackable { get; }

        bool PreventsAction { get; }

        bool IsExpired { get; }

        void Apply(Character target);

        void Tick();

        void Extend(int turns);

        void Refresh(int turns);
    }

    public abstract class StatusEffectBase : IStatusEffect
    {
        public abstract string Name { get; }
        public abstract bool IsStackable { get; }
        public virtual bool PreventsAction => false;

        public int RemainingTurns { get; protected set; }
        public bool IsExpired => RemainingTurns <= 0;

        protected StatusEffectBase(int duration)
        {
            RemainingTurns = duration;
        }

        public abstract void Apply(Character target);

        public void Tick()
        {
            if (RemainingTurns > 0)
            {
                RemainingTurns--;
            }
        }

        public void Extend(int turns)
        {
            RemainingTurns += turns;
        }

        public void Refresh(int turns)
        {
            RemainingTurns = turns;
        }
    }

    public class PoisonEffect : StatusEffectBase
    {
        private readonly int _damage;

        public PoisonEffect(int damage = 5) : base(3)
        {
            _damage = damage;
        }

        public override string Name => "Poison";
        public override bool IsStackable => true;

        public override void Apply(Character target)
        {
            Console.WriteLine($"  [Poison] {target.Name} получает {_damage} урона (броня игнорируется)");
            target.TakeDamage(_damage, ignoreArmor: true);
        }
    }

    public class BurnEffect : StatusEffectBase
    {
        private readonly int _damage;

        public BurnEffect(int damage = 8) : base(2)
        {
            _damage = damage;
        }

        public override string Name => "Burn";
        public override bool IsStackable => false;

        public override void Apply(Character target)
        {
            int dealt = target.TakeDamage(_damage, ignoreArmor: false);
            Console.WriteLine($"  [Burn] {target.Name} получает {dealt} урона (после брони)");
        }
    }

    public class RegenerationEffect : StatusEffectBase
    {
        private readonly int _heal;

        public RegenerationEffect(int heal = 4) : base(4)
        {
            _heal = heal;
        }

        public override string Name => "Regeneration";
        public override bool IsStackable => false;

        public override void Apply(Character target)
        {
            int healed = target.Heal(_heal);
            Console.WriteLine($"  [Regeneration] {target.Name} восстанавливает {healed} HP");
        }
    }

    public class FreezeEffect : StatusEffectBase
    {
        public FreezeEffect() : base(1)
        {
        }

        public override string Name => "Freeze";
        public override bool IsStackable => false;
        public override bool PreventsAction => true;

        public override void Apply(Character target)
        {
            Console.WriteLine($"  [Freeze] {target.Name} заморожен и пропускает действие");
        }
    }

    public class Character
    {
        private readonly List<IStatusEffect> _effects = new List<IStatusEffect>();

        public string Name { get; }
        public int HP { get; private set; }
        public int MaxHP { get; }
        public int Armor { get; }

        public IReadOnlyList<IStatusEffect> ActiveEffects => _effects;

        public bool CanAct => !_effects.Any(e => e.PreventsAction);

        public bool IsAlive => HP > 0;

        public Character(string name, int maxHp, int armor)
        {
            Name = name;
            MaxHP = maxHp;
            HP = maxHp;
            Armor = armor;
        }

        public int TakeDamage(int amount, bool ignoreArmor)
        {
            int damage = ignoreArmor ? amount : Math.Max(0, amount - Armor);
            damage = Math.Min(damage, HP);
            HP -= damage;
            return damage;
        }

        public int Heal(int amount)
        {
            int healed = Math.Min(amount, MaxHP - HP);
            HP += healed;
            return healed;
        }

        public void ApplyEffect(IStatusEffect effect)
        {
            IStatusEffect existing = _effects.FirstOrDefault(e => e.Name == effect.Name);

            if (existing == null)
            {
                _effects.Add(effect);
                Console.WriteLine($"  + {Name}: наложен эффект {effect.Name} ({effect.RemainingTurns} х.)");
            }
            else if (existing.IsStackable)
            {
                existing.Extend(effect.RemainingTurns);
                Console.WriteLine($"  + {Name}: {existing.Name} стакается, теперь {existing.RemainingTurns} х.");
            }
            else
            {
                existing.Refresh(effect.RemainingTurns);
                Console.WriteLine($"  + {Name}: {existing.Name} обновлён, теперь {existing.RemainingTurns} х.");
            }
        }

        public void ProcessTurn()
        {
            foreach (IStatusEffect effect in _effects.ToList())
            {
                effect.Apply(this);
                effect.Tick();
            }

            foreach (IStatusEffect expired in _effects.Where(e => e.IsExpired).ToList())
            {
                Console.WriteLine($"  - эффект {expired.Name} закончился");
            }
            _effects.RemoveAll(e => e.IsExpired);
        }

        public void PrintState()
        {
            string effects = _effects.Count == 0
                ? "нет"
                : string.Join(", ", _effects.Select(e => $"{e.Name}({e.RemainingTurns})"));
            Console.WriteLine($"  Состояние: {Name} HP {HP}/{MaxHP}, броня {Armor}, " +
                              $"может действовать: {(CanAct ? "да" : "нет")}; эффекты: {effects}");
        }
    }

    // ЗАДАЧА 2. Взаимодействие с окружением (двойная диспетчеризация)

    public interface IEntityVisitor
    {
        void Visit(Chest chest);
        void Visit(Trap trap);
        void Visit(Campfire campfire);
    }

    public interface IEntity : IEntityVisitor
    {
        string Name { get; }
        int HP { get; }

        
        void InteractWith(IEnvironmentObject obj);
    }

    public interface IEnvironmentObject
    {
        
        void Accept(IEntity entity);
    }

    

    public class Chest : IEnvironmentObject
    {
        public bool IsOpen { get; set; }
        public bool IsDestroyed { get; set; }
        public int Gold { get; set; } = 50;

        public void Accept(IEntity entity)
        {
            entity.Visit(this);
        }
    }

    public class Trap : IEnvironmentObject
    {
        public bool IsArmed { get; set; } = true;
        public int Damage { get; } = 15;

        public void Accept(IEntity entity)
        {
            entity.Visit(this);
        }
    }

    public class Campfire : IEnvironmentObject
    {
        public bool IsLit { get; set; }

        public void Accept(IEntity entity)
        {
            entity.Visit(this);
        }
    }


    public abstract class EntityBase : IEntity
    {
        public string Name { get; }
        public int HP { get; protected set; }

        protected EntityBase(string name, int hp)
        {
            Name = name;
            HP = hp;
        }

        public void InteractWith(IEnvironmentObject obj)
        {
            obj.Accept(this);
        }

        public abstract void Visit(Chest chest);
        public abstract void Visit(Trap trap);
        public abstract void Visit(Campfire campfire);

        protected void TakeDamage(int amount)
        {
            HP = Math.Max(0, HP - amount);
        }

        protected void Say(string text)
        {
            Console.WriteLine($"  {GetType().Name} {Name}: {text}");
        }
    }

    public class Player : EntityBase
    {
        public int Gold { get; private set; }

        public Player(string name, int hp) : base(name, hp)
        {
        }

        public override void Visit(Chest chest)
        {
            if (chest.IsDestroyed)
            {
                Say("сундук разрушен, брать нечего");
            }
            else if (chest.IsOpen)
            {
                Say("сундук уже открыт и пуст");
            }
            else
            {
                chest.IsOpen = true;
                Gold += chest.Gold;
                Say($"открывает сундук и получает {chest.Gold} золота (всего {Gold})");
                chest.Gold = 0;
            }
        }

        public override void Visit(Trap trap)
        {
            if (trap.IsArmed)
            {
                trap.IsArmed = false;
                Say("обезвреживает ловушку");
            }
            else
            {
                Say("ловушка уже обезврежена");
            }
        }

        public override void Visit(Campfire campfire)
        {
            if (campfire.IsLit)
            {
                Say("костёр уже горит, греется");
            }
            else
            {
                campfire.IsLit = true;
                Say("разжигает костёр");
            }
        }
    }

    public class Enemy : EntityBase
    {
        public Enemy(string name, int hp) : base(name, hp)
        {
        }

        public override void Visit(Chest chest)
        {
            if (chest.IsDestroyed)
            {
                Say("сундук уже разрушен");
            }
            else
            {
                chest.IsDestroyed = true;
                chest.Gold = 0;
                Say("атакует и ломает сундук (открыть он не может)");
            }
        }

        public override void Visit(Trap trap)
        {
            
            if (trap.IsArmed)
            {
                TakeDamage(trap.Damage);
                Say($"попадает в ловушку и получает {trap.Damage} урона (HP {HP})");
            }
            else
            {
                Say("проходит мимо обезвреженной ловушки");
            }
        }

        public override void Visit(Campfire campfire)
        {
            if (campfire.IsLit)
            {
                campfire.IsLit = false;
                Say("тушит костёр");
            }
            else
            {
                Say("костёр и так не горит");
            }
        }
    }

    public class Ally : EntityBase
    {
        public Ally(string name, int hp) : base(name, hp)
        {
        }

        public override void Visit(Chest chest)
        {
            Say("игнорирует сундук");
        }

        public override void Visit(Trap trap)
        {
            if (trap.IsArmed)
            {
                TakeDamage(trap.Damage);
                Say($"попадает в ловушку и получает {trap.Damage} урона (HP {HP})");
            }
            else
            {
                Say("обходит обезвреженную ловушку");
            }
        }

        public override void Visit(Campfire campfire)
        {
            Say(campfire.IsLit ? "греется у костра" : "костёр не горит, ничего не происходит");
        }
    }

    
    // Демонстрация

    public static class Program
    {
        public static void Main()
        {
            RunTask1();
            Console.WriteLine();
            RunTask2();
        }

        private static void RunTask1()
        {
            Console.WriteLine("=== Задача 1. Эффекты состояния ===");

            var hero = new Character("Hero", maxHp: 50, armor: 3);
            hero.PrintState();

            Console.WriteLine("\nНакладываем эффекты:");
            hero.ApplyEffect(new PoisonEffect());
            hero.ApplyEffect(new PoisonEffect());       
            hero.ApplyEffect(new BurnEffect());
            hero.ApplyEffect(new BurnEffect());         
            hero.ApplyEffect(new RegenerationEffect());
            hero.ApplyEffect(new FreezeEffect());
            hero.PrintState();

            for (int turn = 1; turn <= 7; turn++)
            {
                Console.WriteLine($"\n--- Ход {turn} ---");
                Console.WriteLine($"  Может действовать в начале хода: {(hero.CanAct ? "да" : "нет")}");
                hero.ProcessTurn();
                hero.PrintState();
            }
        }

        private static void RunTask2()
        {
            Console.WriteLine("=== Задача 2. Взаимодействие с окружением ===");

            var entities = new List<IEntity>
            {
                new Player("Alice", 100),
                new Enemy("Goblin", 60),
                new Ally("Bob", 80)
            };

            foreach (IEntity entity in entities)
            {
                var objects = new List<IEnvironmentObject>
                {
                    new Chest(),
                    new Trap(),
                    new Campfire()
                };

                foreach (IEnvironmentObject obj in objects)
                {
                    Console.WriteLine($"{entity.Name} -> {obj.GetType().Name}:");
                    entity.InteractWith(obj);
                }
                Console.WriteLine();
            }
        }
    }
}

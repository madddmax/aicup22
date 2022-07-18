using System;
using System.Collections.Generic;
using AiCup22.Model;

namespace AiCup22.Strategy;

public struct MyLoot : IEquatable<MyLoot>
{
    public int Id { get; set; }

    public Vec2 Position { get; set; }

    public MyLootType Type { get; set; }

    public int Amount { get; set; }

    public bool InZone { get; set; }

    public Dictionary<int, double> DistanceSquaredToMyUnit { get; set; }

    public Dictionary<int, bool> InMyUnit { get; set; }

    public MyLoot(Loot loot)
    {
        Id = loot.Id;
        Position = loot.Position;
        Type = MyLootType.MagicWand;
        Amount = 1;

        switch (loot.Item)
        {
            case Item.Weapon weapon:
                switch (weapon.TypeIndex)
                {
                    case 0:
                        Type = MyLootType.MagicWand;
                        Amount = 1;
                        break;
                    case 1:
                        Type = MyLootType.Staff;
                        Amount = 1;
                        break;
                    case 2:
                        Type = MyLootType.Bow;
                        Amount = 1;
                        break;
                }

                break;

            case Item.ShieldPotions potions:
                Type = MyLootType.ShieldPotion;
                Amount = potions.Amount;
                break;

            case Item.Ammo ammo:
                switch (ammo.WeaponTypeIndex)
                {
                    case 0:
                        Type = MyLootType.MagicWandAmmo;
                        Amount = ammo.Amount;
                        break;
                    case 1:
                        Type = MyLootType.StaffAmmo;
                        Amount = ammo.Amount;
                        break;
                    case 2:
                        Type = MyLootType.BowAmmo;
                        Amount = ammo.Amount;
                        break;
                }

                break;
        }

        InZone = false;
        DistanceSquaredToMyUnit = new();
        InMyUnit = new();
    }

    public bool Equals(MyLoot other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object obj)
    {
        return obj is MyLoot other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id);
    }

    public static bool operator ==(MyLoot left, MyLoot right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(MyLoot left, MyLoot right)
    {
        return !left.Equals(right);
    }
}
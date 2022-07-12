using System;
using System.Collections.Generic;
using AiCup22.Model;

namespace AiCup22.Strategy;

public class Context
{
    private readonly Constants _constants;

    public Unit MyUnit;

    public readonly List<MyLoot> Items = new();

    public Context(Constants constants)
    {
        _constants = constants;
    }

    public void Init(Game game)
    {
        Items.Clear();

        foreach (Unit unit in game.Units)
        {
            if (unit.PlayerId != game.MyId)
            {
                continue;
            }

            MyUnit = unit;
        }

        foreach (Loot loot in game.Loot)
        {
            var item = new MyLoot
            {
                Id = loot.Id,
                Position = loot.Position
            };

            switch (loot.Item)
            {
                case Item.Weapon weapon:
                    switch (weapon.TypeIndex)
                    {
                        case 0:
                            item.Type = MyLootType.MagicWand;
                            item.Amount = 1;
                            break;
                        case 1:
                            item.Type = MyLootType.Staff;
                            item.Amount = 1;
                            break;
                        case 2:
                            item.Type = MyLootType.Bow;
                            item.Amount = 1;
                            break;
                    }

                    break;

                case Item.ShieldPotions potions:
                    item.Type = MyLootType.ShieldPotion;
                    item.Amount = potions.Amount;
                    break;

                case Item.Ammo ammo:
                    switch (ammo.WeaponTypeIndex)
                    {
                        case 0:
                            item.Type = MyLootType.MagicWandAmmo;
                            item.Amount = ammo.Amount;
                            break;
                        case 1:
                            item.Type = MyLootType.StaffAmmo;
                            item.Amount = ammo.Amount;
                            break;
                        case 2:
                            item.Type = MyLootType.BowAmmo;
                            item.Amount = ammo.Amount;
                            break;
                    }

                    break;
            }

            var distanceSquaredToZoneCenter = Calc.DistanceSquared(loot.Position, game.Zone.CurrentCenter);
            item.DistanceSquaredToZoneCenter = distanceSquaredToZoneCenter;
            item.InZone = distanceSquaredToZoneCenter <= game.Zone.CurrentRadius * game.Zone.CurrentRadius;

            var distanceSquaredToMyUnit = Calc.DistanceSquared(loot.Position, MyUnit.Position);
            item.DistanceSquaredToMyUnit = distanceSquaredToMyUnit;
            item.InMyUnit = distanceSquaredToMyUnit <= _constants.UnitRadius * _constants.UnitRadius;

            Items.Add(item);
        }
    }
}
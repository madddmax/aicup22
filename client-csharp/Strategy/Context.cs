using System;
using System.Collections.Generic;
using System.Linq;
using AiCup22.Model;

namespace AiCup22.Strategy;

public class Context
{
    private readonly Constants _constants;

    public Unit MyUnit;

    public readonly Dictionary<int, MyLoot> Items = new();

    public Context(Constants constants)
    {
        _constants = constants;
    }

    public void Init(Game game)
    {
        foreach (Unit unit in game.Units)
        {
            if (unit.PlayerId != game.MyId)
            {
                continue;
            }

            MyUnit = unit;
        }

        RemoveDisappearedLoot(game);

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

            Items[item.Id] = item;
        }

        foreach (var item in Items.Values)
        {
            var myLoot = item;

            var distanceSquaredToZoneCenter = Calc.DistanceSquared(myLoot.Position, game.Zone.CurrentCenter);
            myLoot.DistanceSquaredToZoneCenter = distanceSquaredToZoneCenter;
            myLoot.InZone = distanceSquaredToZoneCenter <= game.Zone.CurrentRadius * game.Zone.CurrentRadius;

            var distanceSquaredToMyUnit = Calc.DistanceSquared(myLoot.Position, MyUnit.Position);
            myLoot.DistanceSquaredToMyUnit = distanceSquaredToMyUnit;
            myLoot.InMyUnit = distanceSquaredToMyUnit <= _constants.UnitRadius * _constants.UnitRadius;

            Items[item.Id] = myLoot;
        }
    }

    private void RemoveDisappearedLoot(Game game)
    {
        var removedItems = new List<int>();
        foreach (var item in Items.Values)
        {
            var directionVec = Calc.VecMultiply(MyUnit.Direction, _constants.ViewDistance);

            bool isInSector = Calc.IsInSector(directionVec, Calc.VecDiff(MyUnit.Position, item.Position));
            if (isInSector)
            {
                bool disappeared = game.Loot.All(i => i.Id != item.Id);
                if (disappeared)
                {
                    removedItems.Add(item.Id);
                }
            }
        }

        foreach (var removedId in removedItems)
        {
            Items.Remove(removedId);
        }
    }
}
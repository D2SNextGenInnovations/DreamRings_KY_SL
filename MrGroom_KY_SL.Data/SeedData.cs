using MrGroom_KY_SL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Data
{
    public class SeedData
    {
        public static void Seed(AppDbContext ctx)
        {
            if (!ctx.Staff.Any())
            {
                ctx.Staff.Add(new Staff { Name = "Nimal Perera", Role = "Photographer", Phone = "+94-77-1234567" });
                ctx.Staff.Add(new Staff { Name = "Kamal Fernando", Role = "Editor", Phone = "+94-77-2345678" });
                ctx.SaveChanges();
            }

            if (!ctx.Packages.Any())
            {
                ctx.Packages.Add(new Package { Name = "Gold", Description = "Full day wedding package", BasePrice = 120000 });
                ctx.Packages.Add(new Package { Name = "Silver", Description = "Half day package", BasePrice = 75000 });
                ctx.SaveChanges();
            }

            if (!ctx.EventTypes.Any())
            {
                ctx.EventTypes.Add(new EventType { Name = "Wedding" });
                ctx.EventTypes.Add(new EventType { Name = "Birthday" });
                ctx.SaveChanges();
            }

            if (!ctx.PackageEventTypes.Any())
            {
                var gold = ctx.Packages.FirstOrDefault(p => p.Name == "Gold").PackageId;
                var wedding = ctx.EventTypes.FirstOrDefault(e => e.Name == "Wedding").EventTypeId;
                ctx.PackageEventTypes.Add(new Models.PackageEventType { PackageId = gold, EventTypeId = wedding });
                ctx.SaveChanges();
            }
        }
    }
}

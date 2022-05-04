using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Session.Utils;
using System.IO;

namespace Session.Models
{
    partial class Material
    {
        public string logoPath
        {
            get
            {
                return string.IsNullOrEmpty(Image) ? Directory.GetCurrentDirectory() + "/picture.png" : Directory.GetCurrentDirectory() + Image;
            }
        }

        public string SupArr
        {
            get
            {
                string sup = string.Empty;

                foreach (var i in Suppliers)
                {
                    if (sup != string.Empty)
                        sup += ", ";
                    sup += i.Title;
                }

                return sup;
            }
        }

        public string backColor
        {
            get
            {
                string color = "Transparent";

                if (CountInStock < MinCount)
                    color = "#f19292";
                else if (CountInStock > MinCount * 3)
                    color = "#ffba01";

                return color;
            }
        }

        public string MinBuyCost
        {
            get
            {
                if (CountInStock < MinCount)
                    return (Math.Ceiling(((decimal)MinCount - (decimal)CountInStock) / (decimal)CountInPack) * Cost).ToString();
                else return "0";
            }
        }

        public string MinBuyCount
        {
            get
            {
                if (CountInStock < MinCount)
                    return (Math.Ceiling(((decimal)MinCount - (decimal)CountInStock) / (decimal)CountInPack)).ToString();
                else return "0";
            }
        }
    }
}

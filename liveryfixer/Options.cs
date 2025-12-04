using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace liveryfixer
{
    internal class Options
    {
        public static string packagePathPrefix = "tfdidesign-aircraft-";

        public static Dictionary<string, List<string>> requiredTextureFallbacksByType = new Dictionary<string, List<string>>()
        {
            { "md-11 ge", new List<string>()
                {
                    @"..\..\..\..\texture",
                    @"..\..\..\..\texture\detailMap",
                    @"..\..\..\..\texture\AS1000",
                    @"..\..\..\..\texture\Glass",
                    @"..\..\..\..\texture\Lights",
                    @"..\..\..\..\texture\Planes_Generic",
                    @"..\..\TFDi_Design_MD-11\texture.VC",
                    @"..\..\TFDi_Design_MD-11\texture.BASE",
                    @"..\..\TFDi_Design_MD-11\texture.CABIN",
                    @"..\..\TFDi_Design_MD-11_GE\texture.BASE"
                }
            },

            {
                "md-11f ge", new List<string>()
                {
                    @"..\..\..\..\texture",
                    @"..\..\..\..\texture\detailMap",
                    @"..\..\..\..\texture\AS1000",
                    @"..\..\..\..\texture\Glass",
                    @"..\..\..\..\texture\Lights",
                    @"..\..\..\..\texture\Planes_Generic",
                    @"..\..\TFDi_Design_MD-11\texture.VC",
                    @"..\..\TFDi_Design_MD-11\texture.BASE",
                    @"..\..\TFDi_Design_MD-11\texture.CABIN",
                    @"..\..\TFDi_Design_MD-11F_GE\texture.BASE"
                }
            },

            {
                "md-11f pw", new List<string>()
                {
                    @"..\..\..\..\texture",
                    @"..\..\..\..\texture\detailMap",
                    @"..\..\..\..\texture\AS1000",
                    @"..\..\..\..\texture\Glass",
                    @"..\..\..\..\texture\Lights",
                    @"..\..\..\..\texture\Planes_Generic",
                    @"..\..\TFDi_Design_MD-11\texture.VC",
                    @"..\..\TFDi_Design_MD-11\texture.BASE",
                    @"..\..\TFDi_Design_MD-11\texture.CABIN",
                    @"..\..\TFDi_Design_MD-11F_PW\texture.BASE"
                }
            },

            {
                "md-11 pw", new List<string>()
                {
                    @"..\..\..\..\texture",
                    @"..\..\..\..\texture\detailMap",
                    @"..\..\..\..\texture\AS1000",
                    @"..\..\..\..\texture\Glass",
                    @"..\..\..\..\texture\Lights",
                    @"..\..\..\..\texture\Planes_Generic",
                    @"..\..\TFDi_Design_MD-11\texture.VC",
                    @"..\..\TFDi_Design_MD-11\texture.BASE",
                    @"..\..\TFDi_Design_MD-11\texture.CABIN",
                    @"..\..\TFDi_Design_MD-11_PW\texture.BASE"
                }
            }
        };

        public static Dictionary<string, List<string>> unneededTextureFallbacksByType = new Dictionary<string, List<string>>()
        {
            { 
                "", new List<string>()
                {
                    @"..\..\TFDi_Design_MD-11_GE\texture.VC",
                    @"..\..\TFDi_Design_MD-11_GE\texture.Cabin",
                    @"..\..\TFDi_Design_MD-11F_GE\texture.VC",
                    @"..\..\TFDi_Design_MD-11F_GE\texture.Cabin",
                    @"..\..\TFDi_Design_MD-11F_PW\texture.VC",
                    @"..\..\TFDi_Design_MD-11F_PW\texture.Cabin",
                    @"..\..\TFDi_Design_MD-11_PW\texture.VC",
                    @"..\..\TFDi_Design_MD-11_PW\texture.Cabin"
                }
            },
            { "md-11 ge", new List<string>()
                {
                   @"..\..\TFDi_Design_MD-11F_GE\texture.BASE",
                   @"..\..\TFDi_Design_MD-11F_PW\texture.BASE",
                   @"..\..\TFDi_Design_MD-11_PW\texture.BASE"
                }
            },

            {
                "md-11f ge", new List<string>()
                {
                    @"..\..\TFDi_Design_MD-11_GE\texture.BASE",
                    @"..\..\TFDi_Design_MD-11F_PW\texture.BASE",
                    @"..\..\TFDi_Design_MD-11_PW\texture.BASE"
                }
            },

            {
                "md-11f pw", new List<string>()
                {
                    @"..\..\TFDi_Design_MD-11_GE\texture.BASE",
                    @"..\..\TFDi_Design_MD-11F_GE\texture.BASE",
                    @"..\..\TFDi_Design_MD-11_PW\texture.BASE"
                }
            },

            {
                "md-11 pw", new List<string>()
                {
                    @"..\..\TFDi_Design_MD-11_GE\texture.BASE",
                    @"..\..\TFDi_Design_MD-11F_GE\texture.BASE",
                    @"..\..\TFDi_Design_MD-11F_PW\texture.BASE"
                }
            }
        };

        public static Dictionary<string, string> creatorNameCorrections = new Dictionary<string, string>()
        {
            { "WhiskeyThrottle | HUES", "HUES" },
            { "Hues, TFDi Design", "HUES"},
            { "WhiskeyThrottle, TFDi Design", "HUES" },
            {"SilkySmooth, TFDi Design", "HUES" },
            {"TFDi Design | HUES", "HUES" }
        };
    }
}

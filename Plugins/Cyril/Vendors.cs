
using System.Collections.Generic;
using System.Linq;
using Clio.Utilities;
using ff14bot;
using ff14bot.Managers;

namespace Cyril
{
    public class Vendor
    {
        public uint LocationId { get; set; }

        public Vector3 XYZ { get; set; }

        public Vendor()
        {
            LocationId = 0;
            XYZ = Vector3.Zero;
        }

        public Vendor(uint locationId, Vector3 location)
        {
            LocationId = locationId;
            XYZ = location;
        }

        public static readonly HashSet<uint> NpcIds = new HashSet<uint>()
        {
            1000220,1000222,1000230,1000396,1000397,
            1000535,1000579,1000597,1000717,1000718,
            1001563,1002371,1002372,1002376,1003257,
            1003258,1003259,1003260,1003261,1003262,
            1003263,1003264,1003266,1003267,1003268,
            1003724,1003737,1004038,1004160,1004421,
            1004422,1004426,1004428,1004429,1004430,
            1004598,1005180,1005181,1005182,1005249,
            1006798,1006799,1006800,1006898,1011229,
            1011906,1011913,1011926,1011948,1012062,
            1012074,1012086,1012143,1013732
        };
    }

    public class VendorLocations
    {
        public static List<Vendor> Locations = new List<Vendor>();

        internal static void Populate()
        {
            Locations.Add(new Vendor(134, new Vector3(201.709f, 98.42287f, -206.1036f))); // Summerford Farms
            Locations.Add(new Vendor(135, new Vector3(201.2207f, 14.09601f, 677.4546f))); // Moraby Drydocks
            Locations.Add(new Vendor(135, new Vector3(-1.693787f, 8.921356f, 865.3544f))); // Candlekeep Quay
            Locations.Add(new Vendor(135, new Vector3(554.4365f, 89.02531f, -49.82074f))); // Red Rooster Stead
            Locations.Add(new Vendor(137, new Vector3(-36.2142f, 71.75401f, -37.01686f))); // Wineport
            Locations.Add(new Vendor(137, new Vector3(460.3798f, 17.12659f, 486.5948f))); // Costa Del Sol
            Locations.Add(new Vendor(138, new Vector3(667.4143f, 9.82882f, 494.5602f))); // Swiftperch
            Locations.Add(new Vendor(138, new Vector3(240.2533f, -24.99897f, 231.4336f))); // Aleport
            Locations.Add(new Vendor(138, new Vector3(55.55798f, -3.11828f, 65.65955f))); // Camp Skull Valley
            Locations.Add(new Vendor(138, new Vector3(19.4552f, -10.31952f, -64.10321f))); // North Tidegate
            Locations.Add(new Vendor(138, new Vector3(-90.86749f, -24.78893f, 83.9093f))); // South Tidegate
            Locations.Add(new Vendor(139, new Vector3(-329.6712f, -2.533815f, 136.2782f))); // Memeroon's Trading Post
            Locations.Add(new Vendor(139, new Vector3(234.4854f, -0.9121512f, 248.2795f))); // Jijiroon's Trading Post
            Locations.Add(new Vendor(139, new Vector3(427.2678f, 4.098778f, 115.2819f))); // Camp Bronze Lake
            Locations.Add(new Vendor(140, new Vector3(42.52686f, 45.65809f, -266.8345f))); // Horizon
            Locations.Add(new Vendor(140, new Vector3(207.1716f, 52.03812f, 134.1726f))); // Scorpion Crossing
            Locations.Add(new Vendor(140, new Vector3(-316.64f, 33.27094f, 391.9889f))); // Silver Bazaar Docks
            Locations.Add(new Vendor(140, new Vector3(-306.508f, 18.74717f, -147.1123f))); // Crescent Cove
            Locations.Add(new Vendor(140, new Vector3(-463.1617f, 23.02054f, -379.8878f))); // Vesper bay
            Locations.Add(new Vendor(141, new Vector3(-8.346741f, -2.04808f, -149.3401f))); // Black Brush Station
            Locations.Add(new Vendor(145, new Vector3(-405.7696f, -54.1446f, 97.03169f))); // Camp Drybone
            Locations.Add(new Vendor(145, new Vector3(-533.5012f, 4.599574f, -238.3917f))); // The Golden Bazaar
            Locations.Add(new Vendor(146, new Vector3(-157.3663f, 27.18813f, -434.9279f))); // Little Ala Mhigo
            Locations.Add(new Vendor(146, new Vector3(-278.7976f, 8f, 378.8662f))); // Forgotten Springs
            Locations.Add(new Vendor(147, new Vector3(-26.24441f, 6.9845f, 475.4407f))); // Camp Bluefrog
            Locations.Add(new Vendor(147, new Vector3(-3.250244f, 43.13602f, 32.66956f))); // Ceruleum Processing Plant
            Locations.Add(new Vendor(148, new Vector3(16.18976f, -8.010208f, -15.64056f))); // Bentbranch Meadows
            Locations.Add(new Vendor(148, new Vector3(82.59705f, -7.893894f, -103.3494f))); // The Bannock
            Locations.Add(new Vendor(148, new Vector3(175.616f, -31.9986f, 319.3254f))); // The Mirror Planks
            Locations.Add(new Vendor(152, new Vector3(-213.9468f, 2.324794f, 300.4348f))); // The Hawthorne Hut
            Locations.Add(new Vendor(152, new Vector3(16.03717f, -4.596201f, 220.5081f))); // Little Solace
            Locations.Add(new Vendor(152, new Vector3(-480.9186f, 8.031123f, 201.9226f))); // Fullflower Comb
            Locations.Add(new Vendor(152, new Vector3(-577.0746f, 12.55929f, 102.5664f))); // Sweetbloom Pier
            Locations.Add(new Vendor(153, new Vector3(-214.313f, 21.12917f, 367.2389f))); // Camp Tranquil
            Locations.Add(new Vendor(153, new Vector3(161.9745f, 9.536184f, -60.10535f))); // Quarrymill
            Locations.Add(new Vendor(153, new Vector3(-164.7356f, 9.869228f, -76.69026f))); // Buscarron's Druthers
            Locations.Add(new Vendor(154, new Vector3(8.236346f, -44.73338f, 220.8371f))); // Fallgourd Float
            Locations.Add(new Vendor(154, new Vector3(332.2346f, -5.550671f, 332.4788f))); // Treespeak Stables
            Locations.Add(new Vendor(154, new Vector3(420.7882f, -3.795246f, -123.4023f))); // Hyrstmill
            Locations.Add(new Vendor(155, new Vector3(242.583f, 302f, -256.4067f))); // Camp Dragonhead
            Locations.Add(new Vendor(155, new Vector3(235.1817f, 222.2132f, 321.7134f))); // First Dicasterial Observatorium of Aetherial and Astrological Phenomena
            Locations.Add(new Vendor(155, new Vector3(-413.3898f, 210.8949f, -274.2819f))); // Whitebrim Front
            Locations.Add(new Vendor(156, new Vector3(430.5944f, -5.326402f, -453.0861f))); // Saint Coinach's Find
            Locations.Add(new Vendor(156, new Vector3(48.12084f, 31.1198f, -734.6162f))); // Revenant's Toll
            Locations.Add(new Vendor(180, new Vector3(-102.0371f, 64.20702f, -202.9908f))); // Camp Overlook
            Locations.Add(new Vendor(397, new Vector3(-293.8533f, 126.7501f, 1.35697f))); // The Convictory
            Locations.Add(new Vendor(397, new Vector3(500.305f, 212.5399f, 716.1495f))); // Falcon's Nest
            Locations.Add(new Vendor(398, new Vector3(483.8689f, -51.1414f, 27.0412f))); // Tailfeather
            Locations.Add(new Vendor(398, new Vector3(67.76598f, -49.27008f, -158.7628f))); // Loth ast Vath
            Locations.Add(new Vendor(399, new Vector3(-45.27419f, 100.8346f, -193.9824f))); // Bigwest Shortstop
            Locations.Add(new Vendor(400, new Vector3(291.1092f, -42.78244f, 577.4136f))); // Moghome
            Locations.Add(new Vendor(400, new Vector3(-93.34529f, -8.845936f, 189.3496f))); // Asah
            Locations.Add(new Vendor(401, new Vector3(-648.5139f, -123.882f, 525.7344f))); // Camp Cloudtop
            Locations.Add(new Vendor(401, new Vector3(-563.1339f, -52.54673f, -416.1956f))); // Ok' Zundu
            Locations.Add(new Vendor(402, new Vector3(-638.633f, -176.4502f, -552.5981f))); // Helix
        }

        public static Vector3 Closest
        {
            get
            {
                Vector3 Location = Vector3.Zero;

                foreach (Vendor vendor in Locations.Where(r => r.LocationId == WorldManager.ZoneId))
                {
                    if (Vector3.Distance(Core.Player.Location, vendor.XYZ) < Vector3.Distance(Core.Player.Location, Location))
                        Location = vendor.XYZ;
                }

                return Location;
            }
        }
    }
}

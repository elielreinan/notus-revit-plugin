using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace NotusRevitPlugin.Models
{
    public class HvacCalculationPreview
    {
        public Element Element { get; set; }
        public RoomInput Input { get; set; }
        public HvacResult Result { get; set; }
        public List<string> Alerts { get; set; } = new List<string>();
        public bool CanWrite { get; set; } = true;
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace VisualizeArray
{
    class SettingValue 
    {
        public string Name;
        public FieldInfo Field;
        public string Comment = null;

        static int ParseNum(string val)
        {
            return ScottsUtils.Equation.Equation<int>.Static.Evaluate(val);
        }

        public string Value
        {
            get
            {
                if (Field.FieldType.IsAssignableFrom(typeof(string)))
                {
                    return (string)Field.GetValue(null);
                }
                else if (Field.FieldType.IsAssignableFrom(typeof(int)))
                {
                    return ((int)Field.GetValue(null)).ToString();
                }
                else if (Field.FieldType.IsAssignableFrom(typeof(bool)))
                {
                    return ((bool)Field.GetValue(null)).ToString();
                }
                else
                {
                    throw new Exception();
                }
            }
            set
            {
                if (Field.FieldType.IsAssignableFrom(typeof(string)))
                {
                    Field.SetValue(null, value);
                }
                else if (Field.FieldType.IsAssignableFrom(typeof(int)))
                {
                    int temp = ParseNum(value);
                    Field.SetValue(null, temp);
                }
                else if (Field.FieldType.IsAssignableFrom(typeof(bool)))
                {
                    bool temp = bool.Parse(value);
                    Field.SetValue(null, temp);
                }
                else
                {
                    throw new Exception();
                }
            }
        }
    }
}

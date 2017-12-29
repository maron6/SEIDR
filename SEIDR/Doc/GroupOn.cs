using System;
using System.Collections.Generic;

namespace SEIDR.Doc
{
    /// <summary>
    /// For grouping data that's being processed by a processor. Implicitly gathers some aggregate data from the data when added, based on the type assigned to the GroupOn.
    /// </summary>
    /// <remarks>The average aggregation data is not exact and is instead an approximate average ONLY.
    /// <para>
    /// The GroupOn should be used consistently with the same fields or groupings of fields. If you are combining fields as a group, you should have an identifier to make sure that there are no cases like '430' + '14' matching '4' + '314'. Depending on the data, it's probably not advisable to combine fields as data unless you're creating a derived field and know what you're doing.
    /// </para>
    /// </remarks>
    public class GroupOn
    {
        /// <summary>
        /// Checks if two groupOns have the same name. Description is ignored actually.
        /// </summary>
        /// <param name="g"></param>
        /// <returns></returns>
        public bool Matches(GroupOn g)
        {
            if (g.Name == _Name && g.type == _type)
                return true;
            return false;
        }
        /// <summary>
        /// Constructor + Description
        /// </summary>
        /// <param name="GroupName">Name for identifying this object. It should describe the type of data being grouped, with keys being the actual values grouping.</param>
        /// <param name="type">Type of object action to take when adding a string to the group's data</param>
        /// <param name="GroupDescription">Optional, extra description to describe the goal of the GroupOn object</param>
        public GroupOn(string GroupName, GroupType type, string GroupDescription)
        {
            _Name = GroupName;
            this._type = (int)type;
            _Description = GroupDescription;
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="GroupName">Name for identifying this object. It should describe the type of data being grouped, with keys being the actual values grouping.</param>
        /// <param name="type">Type of object action to take when adding a string to the group's data</param>
        public GroupOn(string GroupName, GroupType type)
        {
            _Name = GroupName;
            this._type = (int)type;
            _Description = "";
            if (type == GroupType.dateType)
            {
                df = new DateFormatter(1);
            }
        }
        /// <summary>
        /// Compares two GroupOns to see if they 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool Compare(GroupOn a, GroupOn b)
        {
            return a.Matches(b);
        }
        DateFormatter df;
        /// <summary>
        /// How data is stored when you add an item to the group. Also determines the type of object returned when you want to get a dataObject for a given key.
        /// </summary>
        public enum GroupType
        {
            /// <summary>
            /// double[4]: Min, Max, average. Conversion fail
            /// </summary>
            NumRange,
            /// <summary>
            /// int[4]: Count less than 0, Count == 0, Count greater than 0, conversion fail
            /// </summary>
            NumSign,
            /// <summary>
            /// int[2]: Min Length, Max Length
            /// </summary>
            varchar,
            /// <summary>
            /// datetime[2]: min date, max date
            /// </summary>
            dateType,
            /// <summary>
            /// List (string): just holds onto every piece of fieldDate added in a list of strings
            /// </summary>
            Record
        };
        int _type;
        /// <summary>
        /// Numeric representation of internal property '_type'. Mainly used for comparison between groupOn objects
        /// </summary>
        public int type { get { return _type; } }
        string _Name;
        /// <summary>
        /// Name representing this groupOn. Mainly used for comparison between groupOn objects
        /// </summary>
        public string Name { get { return _Name; } }
        string _Description;
        List<dataObj> data = new List<dataObj>();
        /*
         * Depending on function/group type: Num: data[0] = <0, data[1] = == 0, data[2] = > 0
         * Num: data[0] = Min, data[1] = max, data[2] = running average
         * varchar: data[0] = min length, data[1] = max length
         * date: data[0] = min date, data[1] = max date.
         * Record: data[0...n] = field for record[0...n]

        */
        /// <summary>
        /// Adds the content of a string to a data object that matches based on the key( Case insensitive, trimmed spaces).
        /// <para>Type of object in data depends on the GroupType chosen at construction.</para>
        /// </summary>
        /// <param name="key">Key for finding the group/data Object we want to update.</param>
        /// <param name="fieldData">Content from a line of raw data that we want to use to update the data</param>
        public void AddData(string key, string fieldData)
        {
            key = key.ToLower().Trim();
            foreach (var d in data)
            {
                if (d.key == key)
                {
                    ModifyData(fieldData.Trim(), d);
                    return;
                }

            }
            dataObj temp = new dataObj(key);
            NewData(fieldData, temp);
            data.Add(temp);
        }
        private void ModifyData(string fieldData, dataObj data)
        {
            switch ((GroupType)_type)
            {
                case (GroupType.NumRange):
                    {
                        double[] t = (double[])data.data;
                        Double s;
                        if (Double.TryParse(fieldData, out s))
                        {
                            if (s < t[0])
                                t[0] = s;
                            if (s > t[1])
                                t[1] = s;
                            t[2] = (t[2] + s) / 2;
                        }
                        else
                        {
                            t[3]++;
                        }
                        data.data = t;
                        return;
                    }
                case (GroupType.NumSign):
                    {
                        int[] t = (int[])data.data;
                        Double s;
                        if (Double.TryParse(fieldData, out s))
                        {
                            t[0] = t[0] + (s < 0 ? 1 : 0);
                            t[1] = t[1] + (s == 0 ? 1 : 0);
                            t[2] = t[2] + (s > 0 ? 1 : 0);
                        }
                        else
                        {
                            t[3]++;
                        }
                        data.data = t;
                        return;
                    }
                case (GroupType.varchar):
                    {
                        int[] t = (int[])data.data;
                        int l = fieldData.Length;
                        if (l < t[0])
                            t[0] = l;
                        if (l > t[1])
                            t[1] = l;
                        data.data = t;
                        return;
                    }
                case (GroupType.dateType):
                    {
                        DateTime[] t = (DateTime[])data.data;
                        DateTime d;
                        if (df.ParseString(1, fieldData, out d))
                        {
                            if (t[0] == null || t[0].Subtract(d).Days > 0)
                                t[0] = d;
                            if (t[1] == null || t[1].Subtract(d).Days < 0)
                                t[1] = d;
                        }
                        data.data = t;
                        return;
                    }
                case (GroupType.Record):
                    {
                        List<string> t = (List<string>)data.data;
                        t.Add(fieldData);
                        data.data = t;
                        return;
                    }
            }
        }
        /// <summary>
        /// Initialize the object inside before adding it to the data list
        /// </summary>
        /// <param name="fieldData">Data used to modify object</param>
        /// <param name="data">Data object to modify</param>
        private void NewData(string fieldData, dataObj data)
        {
            switch ((GroupType)_type)
            {
                case (GroupType.NumRange):
                    {
                        // Num: data[0] = Min, data[1] = max, data[2] = average data[3] = conversion fail
                        Double[] t = new Double[4];
                        Double s;
                        if (Double.TryParse(fieldData, out s))
                        {
                            t[0] = s;
                            t[1] = s;
                            t[2] = s;
                            t[3] = 0;
                        }
                        else
                        {
                            t[0] = Double.MaxValue;
                            t[1] = Double.MinValue;
                            t[2] = 0;
                            t[3] = 1;
                        }
                        data.data = t;
                        return;
                    }
                case (GroupType.NumSign):
                    {
                        //Num: data[0] = <0, data[1] = == 0, data[2] = > 0 data[3] = conversion fail
                        int[] t = new int[4];
                        Double s;
                        if (Double.TryParse(fieldData, out s))
                        {
                            t[0] = s < 0 ? 1 : 0;
                            t[1] = s == 0 ? 1 : 0;
                            t[2] = s > 0 ? 1 : 0;
                            t[3] = 0;
                        }
                        else
                        {
                            t[0] = 0; t[1] = 0; t[2] = 0;
                            t[3] = 1;
                        }
                        data.data = t;
                        return;
                    }
                case (GroupType.varchar):
                    {
                        // varchar: data[0] = min length, data[1] = max length
                        int[] t = new int[2];
                        t[0] = fieldData.Length;
                        t[1] = fieldData.Length;
                        data.data = t;
                        return;
                    }
                case (GroupType.dateType):
                    {
                        //Min/Max dates, no conversion fail.
                        DateTime[] t = new DateTime[2];
                        DateTime d;
                        if (df.ParseString(1, fieldData, out d))
                        {
                            t[0] = d;
                            t[1] = d;
                        }
                        data.data = t;
                        return;
                    }
                case (GroupType.Record):
                    {
                        //List of all field datas associated with the key.
                        List<string> t = new List<string>();
                        t.Add(fieldData);
                        data.data = t;
                        return;
                    }
            }
        }
        /// <summary>
        /// Override object's ToString
        /// </summary>
        /// <returns>String representing the object</returns>
        public override string ToString()
        {
            string t = this._Name + ": " + this._Description + "      Type: " + (GroupType)_type + "      # Objects contained: " + this.data.Count;
            return t;
        }
        /// <summary>
        /// Gets the data for a specific key. You need to know what type of data it is based on what type of group on you chose.
        /// <para>Record: List(string), varchar: int[2], dateType: datetime[2], numrange: double[4], numSign: int[4]</para>
        /// </summary>
        /// <param name="key">Key for finding the data object holding the data you want.</param>
        /// <returns>Object containing the data associated with this key.</returns>
        public object GetObject(string key)
        {
            foreach (var d in data)
            {
                if (d.key == key)
                    return d.data;
            }
            return null;
        }
        class dataObj
        {
            string _key;
            public string key { get { return _key; } }
            public object data;
            public dataObj(string key) { _key = key; data = null; }
        }
    }
}

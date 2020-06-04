using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace IntelogyCommonHelper
{
    public class DataReader
    {
        public IDataRecord Data;
        public int Index = 0;
        public DataReader(IDataRecord data)
        {
            Data = data;
        }

        public DataReader()
        {

        }

        public int GetInt()
        {
            return Data.GetInt(Index++);
        }

        public double GetDouble()
        {
            return Data.GetDbl(Index++);
        }

        public long GetLong()
        {
            return Data.GetLong(Index++);
        }

        public DateTime GetDateTime()
        {
            return Data.GetDate(Index++);
        }

        public decimal GetCurrency()
        {
            return Data.GetCurrency(Index++);
        }

        public string GetText()
        {
            return Data.GetText(Index++);
        }

        public bool GetBool()
        {
            return Data.GetBool(Index++);
        }
    }
}

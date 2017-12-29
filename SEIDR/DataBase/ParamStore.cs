﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace SEIDR.DataBase
{
    public class ParamStore
    {
        public ParamStore()
        {            
            cmdParams = new Dictionary<string, SqlParameter[]>();
        }
        
        public void Remove(SqlCommand cmd)
        {
            string Key = cmd.CommandText.Replace("[", "").Replace("]", "").ToUpper(); ;
            if (cmdParams.ContainsKey(Key))
                cmdParams.Remove(Key);
        }
        Dictionary<string, SqlParameter[]> cmdParams;
        public void FillParameterCollection(SqlCommand cmd)
        {
            if (cmd.CommandType != System.Data.CommandType.StoredProcedure)
                return;
            if (cmd.Connection == null)
                throw new ArgumentException("SqlCommand does not have connection set");
            if (string.IsNullOrWhiteSpace(cmd.CommandText))
                throw new ArgumentException("SqlCommand has invalid command text");            
            string Procedure = cmd.CommandText;
            //ParamStore will be per DatabaseManager, so connection doesn't need to be part of the key            
            string Key =  Procedure.Replace("[", "").Replace("]", "").ToUpper(); 
            SqlParameter[] spc;            
            if (cmdParams.TryGetValue(Key, out spc))
            {
                foreach (SqlParameter parm in spc)
                {
                    SqlParameter clone = new SqlParameter
                    {
                        ParameterName = parm.ParameterName
                        ,UdtTypeName = parm.UdtTypeName
                        ,TypeName = parm.TypeName
                        ,IsNullable = parm.IsNullable                        
                        ,Size = parm.Size
                        ,Direction = parm.Direction
                        ,Precision = parm.Precision
                        ,Scale = parm.Scale
                        ,DbType = parm.DbType
                        ,SqlDbType = parm.SqlDbType
                    };
                    cmd.Parameters.Add(clone);
                }
                return;
            }                            
            SqlCommandBuilder.DeriveParameters(cmd);
            spc = new SqlParameter[cmd.Parameters.Count];
            cmd.Parameters.CopyTo(spc, 0);
            cmdParams[Key] = spc;
        }
        
    }

}
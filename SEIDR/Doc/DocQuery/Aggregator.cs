using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR;

namespace SEIDR.Doc.DocQuery
{
    public class Aggregator
    {

        IEnumerable<DocRecordColumnInfo> GroupBy;
        IEnumerable<AggregationArg> Args;
        Dictionary<ulong, Dictionary<DocRecordColumnInfo, Aggregation>> records;
        Dictionary<ulong, AggregationInfo> AggInfo;
        public void Aggregate(DocRecord add)
        {
            ulong hash = add.GetPartialHash(true, false, true, GroupBy.ToArray()).Value;
            AggregationInfo aggregate;
            if (!AggInfo.TryGetValue(hash, out aggregate))
            {
                aggregate = new AggregationInfo(this, Args);
                AggInfo[hash] = aggregate;
            }
            aggregate.Aggregate(add);
            //Dictionary<DelimitedRecordColumnInfo, Aggregation> aggRecords;
            //if (!records.TryGetValue(hash, out aggRecords))
            //    aggRecords = new Dictionary<DelimitedRecordColumnInfo, Aggregation>();
        }
        public Aggregator(IEnumerable<DocRecordColumnInfo> groupBy, IEnumerable<AggregationArg> args)
        {
            GroupBy = groupBy;
            Args = args;
            foreach(var g in GroupBy)
            {
                if (args.Exists(a => a.AggregateColumn == g))
                    throw new ArgumentException(g.ToString(true) + " - Cannot be used in both group by and an aggregation function");
            }

        }
        public Dictionary<DocRecordColumnInfo, Aggregation> Check(DocRecord compare, bool removeOnFound = true)
        {
            Dictionary<DocRecordColumnInfo, Aggregation> g;
            //Aggregation g;
            var h = compare.GetPartialHash(true, false, true, GroupBy.ToArray()) ?? 0;
            if (!records.TryGetValue(h, out g))
            {
                return null;
            }
            if (removeOnFound)
                records.Remove(h);
            return g;
        }
    }
    public class AggregationInfo
    {
        IEnumerable<AggregationArg> Args;
        public AggregationInfo(Aggregator caller, IEnumerable<AggregationArg> args)
        {
            owner = caller;
            Args = args;
            aggregations = new Dictionary<DocRecordColumnInfo, Aggregation>();
            foreach (var arg in Args)
                aggregations[arg.AggregateColumn] = new Aggregation();
        }
        Aggregator owner;
        public ulong FullCount { get; private set; } = 0;
        public void Reset()
        {
            FullCount = 0;
            aggregations.Clear();
            foreach (var arg in Args)
                aggregations[arg.AggregateColumn] = new Aggregation();
        }
        public Aggregation this[DocRecordColumnInfo column]
            => aggregations[column];
        Dictionary<DocRecordColumnInfo, Aggregation> aggregations;
        public void Aggregate(IRecord toAdd)
        {
            FullCount++;
            foreach (var arg in Args)
            {
                Aggregation agg = aggregations[arg.AggregateColumn];                
                switch (arg.ColumnMetaData.Type)
                {
                    case DataType.DATE:
                        agg.UpdateDate(arg.operation, arg.ColumnMetaData.GetColumn(toAdd).Content as TransformedDate);
                        break;
                    case DataType.MONEY:
                        agg.UpdateMoney(arg.operation, arg.ColumnMetaData.GetColumn(toAdd).Content as TransformedMoney);
                        break;
                    case DataType.NUMBER:
                        agg.UpdateNumber(arg.operation, arg.ColumnMetaData.GetColumn(toAdd).Content as TransformedNum);
                        break;

                    case DataType.VARCHAR:
                    case DataType.DBNULL:
                    default:
                        agg.Update(arg.operation, arg.ColumnMetaData.GetColumn(toAdd).Content);
                        break;
                }
            }
        }
    }
    public class Aggregation
    {
        string valueS = null;
        long? valueL = null;
        /// <summary>
        /// Number of non null records matched to this aggregation
        /// </summary>
        public long Counter { get; private set; } = 0;
        DateTime? valueD = null;
        double calc = 0; //average for date/number
        decimal? money = null;
        public decimal? GetMoney(AggregationType op)
        {            
            return money;
        }
        public DateTime? GetDate(AggregationType op)
        {
            if (op == AggregationType.MOVING_AVERAGE)
                return DateTime.FromOADate(calc);

            return valueD;
        }
        public long? GetNum(AggregationType op)
        {
            return valueL;
        }
        public double GetNumAverage()
            => calc;
        public void UpdateDate(AggregationType op, TransformedDate content)
        {
            if (!content.Content.HasValue)
                return;
            var d = content.Content.Value;
            Counter++;
            if (valueD == null)
            {
                valueD = d;
                return;
            }
            switch (op)
            {
                case AggregationType.MIN:
                    {                                                
                        valueD = d < valueD ? d : valueD;                                                   
                        break;
                    }
                case AggregationType.MAX:
                    {
                        if (d > valueD)
                            valueD = d;
                        break;
                    }                
                case AggregationType.MOVING_AVERAGE:
                    {
                        double work = d.ToOADate();
                        calc = calc.Average(Counter -1, work, 1);
                        //double work2 = (work - Counter) / Counter;
                        //work /= Counter;
                        //calc = calc + work - work2;                        
                        //rolling average?
                        break;
                    }
            }
        }

        public void UpdateMoney(AggregationType op, TransformedMoney content)
        {
            decimal work;
            if (content.Content.HasValue)
                work = content.Content.Value;
            else
                return;
            Counter++;
            if (money == null)
            {
                money = work;
                return;
            }
            switch (op)
            {
                case AggregationType.MIN:
                    if (work < money)
                        money = work;
                    break;
                case AggregationType.MAX:
                    if (work > money)
                        money = work;
                    break;
                case AggregationType.SUM:
                    money += work;
                    break;
                case AggregationType.MOVING_AVERAGE: 
                    money = money.Value.Average(Counter - 1, work, 1);
                    break;
            }
        }
        public void Update(AggregationType op, TransformedData content)
        {

        }
        public void UpdateNumber(AggregationType op, TransformedNum content)
        {
            long work;
            if (content.Content.HasValue)
                work = content.Content.Value;
            else
                return;
            Counter++;
            if (valueL == null)
            {
                if (money == null)
                    money = work;
                valueL = work;
                return;
            }
            switch (op)
            {
                case AggregationType.MIN:
                    if (work < valueL)
                        valueL = work;
                    if (work < money)
                        money = work;
                    return;
                case AggregationType.MAX:
                    if (work > valueL)
                        valueL = work;
                    if (work > money)
                        money = work;
                    return;
                case AggregationType.SUM:
                    valueL += work;
                    money += work;
                    return;
                case AggregationType.MOVING_AVERAGE:                    
                    {
                        calc = calc.Average(Counter - 1, work);
                        double work2 = (work - Counter) / Counter;
                        work /= Counter;
                        calc = calc + work - work2;
                        //rolling average?
                        break;
                    }
            }
        }

    }
    public class AggregationArg
    {
        public AggregationType operation { get; set; }
        public DocRecordColumnInfo AggregateColumn { get; set; }
        public TransformedColumnMetaData ColumnMetaData { get; set; }
    }
    public enum AggregationType
    {
        MAX,
        MIN,
        SUM,
        COUNT,
        MOVING_AVERAGE
    }
}

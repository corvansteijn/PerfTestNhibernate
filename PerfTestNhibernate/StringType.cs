using NHibernate.SqlTypes;

namespace PerfTestNhibernate
{
    public class StringType : AbstractStringType
    {
        public override string Name
        {
            get
            {
                return "String";
            }
        }

        public StringType()
            : base((SqlType)new StringSqlType())
        {
        }

        public StringType(StringSqlType sqlType)
            : base((SqlType)sqlType)
        {
        }
    }
}
using System;
using System.Data;
using NHibernate;
using NHibernate.Dialect;
using NHibernate.SqlTypes;
using NHibernate.Type;

namespace PerfTestNhibernate
{
    [Serializable]
    public abstract class AbstractStringType : ImmutableType, IDiscriminatorType, IIdentifierType, IType, ICacheAssembler, ILiteralType
    {
        public override Type ReturnedClass
        {
            get
            {
                return typeof(string);
            }
        }

        public AbstractStringType(SqlType sqlType)
            : base(sqlType)
        {
        }

        public override void Set(IDbCommand cmd, object value, int index)
        {
            IDbDataParameter dbDataParameter = (IDbDataParameter)cmd.Parameters[index];
            dbDataParameter.Value = value;
            if (dbDataParameter.Size > 0 && ((string)value).Length > dbDataParameter.Size)
                throw new HibernateException("The length of the string value exceeds the length configured in the mapping/parameter.");
        }

        public override object NullSafeGet(IDataReader rs, string name)
        {
            int ordinal = rs.GetOrdinal(name);

            return rs.GetString(ordinal);
        }

        public override object Get(IDataReader rs, int index)
        {
            return rs.GetString(index);
        }

        public override object Get(IDataReader rs, string name)
        {
            int ordinal = rs.GetOrdinal(name);

            return rs.GetString(ordinal);
        }

        public override string ToString(object val)
        {
            return (string)val;
        }

        public override object FromStringValue(string xml)
        {
            return (object)xml;
        }

        public object StringToObject(string xml)
        {
            return (object)xml;
        }

        public string ObjectToSQLString(object value, Dialect dialect)
        {
            return "'" + (string)value + "'";
        }
    }
}
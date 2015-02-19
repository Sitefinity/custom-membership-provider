using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web.Security;
using Telerik.Sitefinity.Data.Linq.Basic;
using Telerik.Sitefinity.Security;
using Telerik.Sitefinity.Security.Model;

namespace SitefinityWebApp.Providers
{
    public class AspNetSqlMembershipProvider : SqlMembershipProvider, IBasicQueryExecutor
    {
        #region Privates variables
        private string appName = "/CustomMembershipProvider";
        #endregion

        #region Properties
        /// <summary>
        /// Application Name
        /// </summary>
        public override string ApplicationName
        {
            get
            {
                return this.appName;
            }

            set
            {
                this.appName = value;
            }
        }

        /// <summary>
        /// Name Provider
        /// </summary>
        public override string Name
        {
            get
            {
                return "AspNetSqlMembershipProvider";
            }
        }
        #endregion

        /// <summary>
        /// Execute a query expression
        /// </summary>
        /// <param name="args">expression tree defined by LINQ and supplied as an argument</param>
        /// <returns>return all found users as Sitefinity users</returns>
        object IBasicQueryExecutor.Execute(QueryArgs args)
        {
            var suffixSql = new StringBuilder();
            var parameters = new List<SqlParameter>();
            var paramName = string.Empty;
            var filters = args.Filters;
            var tableFieldMappings = new Dictionary<string, string>()
            {
                { "Email", "m.Email" },
                { "PasswordQuestion", "m.PasswordQuestion" },
                { "Comment", "m.Comment" },
                { "IsApproved", "m.IsApproved" },
                { "CreateDate", "m.CreateDate" },
                { "LastLoginDate", "m.LastLoginDate" },
                { "LastPasswordChangedDate", "m.LastPasswordChangedDate" },
                { "IsLockedOut", "m.IsLockedOut" },
                { "LastLockoutDate", "m.LastLockoutDate" },

                { "UserName", "u.UserName" },
                { "LastActivityDate", "u.LastActivityDate" },
                { "UserId", "u.UserId" },
                { "Id", "u.UserId" }

            };

            foreach (var filter in filters)
            {
                if (filter.Value is String)
                {
                    if (filter.Action == QueryArgs.Constants.String.Equals)
                    {
                        paramName = "@" + filter.Member;

                        suffixSql.Append("AND {0} = {1} ".Arrange(tableFieldMappings[filter.Member], paramName));
                        parameters.Add(new SqlParameter(paramName, filter.Value));
                    }
                    if (filter.Action == QueryArgs.Constants.String.Contains)
                    {
                        paramName = "@" + filter.Member;

                        suffixSql.Append("AND {0} LIKE '%' + {1} + '%' ".Arrange(tableFieldMappings[filter.Member], paramName));
                        parameters.Add(new SqlParameter(paramName, filter.Value));
                    }
                    if (filter.Action == QueryArgs.Constants.String.StartsWith)
                    {
                        paramName = "@" + filter.Member;
                        var paramWithUpper = paramName;
                        // when searching for a user with a username in the front-end, the query is x.Username.ToUpper().Contains("test")
                        if (filter.Member.Action == "ToUpper")
                            paramWithUpper = string.Format("UPPER({0})", paramName);
                        suffixSql.Append("AND {0} LIKE {1} + '%' ".Arrange(tableFieldMappings[filter.Member], paramWithUpper));
                        parameters.Add(new SqlParameter(paramName, filter.Value));
                    }
                    if (filter.Action == QueryArgs.Constants.String.EndsWith)
                    {
                        paramName = "@" + filter.Member;
                        suffixSql.Append("AND {0} LIKE '%' + {1} ".Arrange(tableFieldMappings[filter.Member], paramName));
                        parameters.Add(new SqlParameter(paramName, filter.Value));
                    }
                }
                else if (filter.Value is IEnumerable && filter.Action == QueryArgs.Constants.Enumerable.Contains)
                {
                    string parameter = string.Join("','", (filter.Value as IEnumerable).Cast<object>().ToArray());
                    suffixSql.Append("AND {0} in ('{1}')".Arrange(tableFieldMappings[filter.Member], parameter));
                }
            }

            if (args.LastAction == QueryArgs.Constants.Queryable.List)
            {
                return this.RetriveMembershipUsers(args, suffixSql, parameters)
                    .Select(x => UserWrapper.CopyFrom(x, this.ApplicationName));
            }
            else if (args.LastAction == QueryArgs.Constants.Queryable.Count)
            {
                string query = String.Format(
                    @"SELECT COUNT(*) FROM dbo.aspnet_Membership m, dbo.aspnet_Users u
                    WHERE  u.UserId = m.UserId {0}", suffixSql);

                return this.WithSqlCommand((cmd) =>
                {
                    return (int)cmd.ExecuteScalar();
                }, query, parameters);
            }
            else if (args.LastAction == QueryArgs.Constants.Queryable.Any)
            {
                string query = String.Format(
                    @"SELECT COUNT(*) FROM dbo.aspnet_Membership m, dbo.aspnet_Users u
                    WHERE  u.UserId = m.UserId {0}", suffixSql);

                return this.WithSqlCommand((cmd) =>
                {
                    return (int)cmd.ExecuteScalar() > 0;
                }, query, parameters);
            }

            return null;
        }

        private TResult WithSqlCommand<TResult>(Func<SqlCommand, TResult> action, string query, IEnumerable<SqlParameter> parameters)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["AspNetMembership"].ConnectionString;

            using (var conn = new SqlConnection(connectionString))
            {
                var cmd = new SqlCommand(query, conn);

                foreach (var parameter in parameters)
                {
                    cmd.Parameters.AddWithValue(parameter.ParameterName, parameter.Value);
                }

                conn.Open();
                return action(cmd);
            }
        }

        /// <summary>
        /// Helper method to retrieve the data for users from DB as SQL query
        /// </summary>
        /// <param name="suffixSql">arguments like Filters, Order by and etc.</param>
        /// <param name="parameters">parameters for particular columns</param>
        private IList<MembershipUser> RetriveMembershipUsers(QueryArgs args, StringBuilder suffixSql = null, List<SqlParameter> parameters = null)
        {
            bool orderByAppended = false;
            if (args.OrderArgs.MemberName != null)
            {
                orderByAppended = true;
                var orderByDirection = args.OrderArgs.Direction == QueryArgs.Order.Directions.Ascending ? "ASC" : "DESC";
                suffixSql.Append("ORDER BY u.{0} {1} ".Arrange(args.OrderArgs.MemberName, orderByDirection));
            }

            if (!orderByAppended)
            {
                //Add Order by u.UserId (some parameter) if no have any order in expression
                suffixSql.Append("order by u.UserId ");
            }

            int skip = 0, take = 0;
            if (args.PagingArgs.Skip != null)
                skip = args.PagingArgs.Skip.Value;
            if (args.PagingArgs.Take != null)
                take = args.PagingArgs.Take.Value;

            if (args.PagingArgs.Take == null)
            {
                suffixSql.Append("OFFSET {0} ROWS ".Arrange(skip));
            }
            else
            {
                suffixSql.Append("OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY ".Arrange(skip, take));
            }

            string query = String.Format(
                    @"SELECT u.UserName, m.Email, m.PasswordQuestion, m.Comment, m.IsApproved,
                            m.CreateDate, m.LastLoginDate, u.LastActivityDate, m.LastPasswordChangedDate,
                            u.UserId, m.IsLockedOut, m.LastLockoutDate FROM dbo.aspnet_Membership m, dbo.aspnet_Users u
                            WHERE  u.UserId = m.UserId {0}", suffixSql);

            return this.WithSqlCommand((cmd) =>
            {
                var users = new List<MembershipUser>();
                var dr = cmd.ExecuteReader();
                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        if (!dr.IsDBNull(0))
                        {
                            users.Add(new MembershipUser(this.Name, dr.GetString(dr.GetOrdinal("UserName")), (object)dr.GetGuid(dr.GetOrdinal("UserId")),
                                dr.IsDBNull(dr.GetOrdinal("Email")) ? null : dr.GetString(dr.GetOrdinal("Email")),
                                dr.IsDBNull(dr.GetOrdinal("PasswordQuestion")) ? null : dr.GetString(dr.GetOrdinal("PasswordQuestion")),
                                dr.IsDBNull(dr.GetOrdinal("Comment")) ? null : dr.GetString(dr.GetOrdinal("Comment")),
                                dr.GetBoolean(dr.GetOrdinal("IsApproved")),
                                dr.GetBoolean(dr.GetOrdinal("IsLockedOut")),
                                dr.GetDateTime(dr.GetOrdinal("CreateDate")),
                                dr.GetDateTime(dr.GetOrdinal("LastLoginDate")),
                                dr.GetDateTime(dr.GetOrdinal("LastActivityDate")),
                                dr.GetDateTime(dr.GetOrdinal("LastPasswordChangedDate")),
                                dr.GetDateTime(dr.GetOrdinal("LastLockoutDate"))));
                        }
                    }

                }
                return users;
            }, query, parameters);
        }

    }
}
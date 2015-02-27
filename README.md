Custom membership provider integration in Sitefinity
==========================
Authentication is an integral part of every web application. In many scenarios, Sitefinity's built-in membership provider (**SitefinityMembershipProvider**) is sufficient for providing the required authentication functionality. However, you may want to integrate your own custom membership provider, so that you reuse your current database records with other web applications. Or, you may want to migrate from standard ASP applications to Sitefinity and wish to reuse your current authentication store.

Registering such custom membership providers in the **web.config** file (as in the standard ASP applications) works OK in a small numbe of cases, but will cause an overhead in most cases and scenarios (~ 10 000 or above). The reason is that the **MembershipProvider** class provides only a single method for filtering users - the **GetAllUsers(int pageIndex, int pageSize, out int totalRecords)** method, which, as its signature states, uses only paginated data with no support for filtering, seaching, or sorting. This functionality is not sufficient and does not serve well Sitefinity's user interface, which supports filtering, sorting, searching, and paging of user records. Thus, you benefit from:
* Loading a small amount of users in memory
* Faster user management

So, how do you fetch filtered, ordered, and paged data from your custom membership provider?
With Sitefinity version 8.0, you can configure custom membership providers to support such funcionality by using the **IBasicQueryExecutor** interface.

### IBasicQueryExecutor
This interface exposes a single method - **Execute(QueryArgs args)**. When decorated on a custom memberip provider, this method is called whenever data needs to be retireved from the custom membership provider with the arguments for filtering, paging, sorting, and search, provided in the **args** variable.

## Custom MembershipProvider example
The example in this repository is a fully functional implementation of the standard **SqlMembershipProvider** with support for filtering, paging, search, and sorting. The example demonstrates you can integrate your custom membership provider to support the user interface of Sitefinity.

## Prerequisities
* A custom membership provider
* Sitefinity 8.0 or above

## API Overview
###QueryArgs
The QueryArgs type has several properties which hold the accumulated data from the Linq query expression. Following are more details on the specifics of each property of the QueryArgs object.

<table>
	<thead>
		<tr>
			<td><strong>Property</strong></td>
			<td><strong>Description</strong></td>
			<td><strong>Query example(s)</strong></td>
		</tr>
	</thead>
	<tbody>
		<tr>
			<td><strong>PagingArgs</strong></td>
			<td>
				<p>
					This property has two sub-properties – <strong>Skip</strong> and <strong>Take</strong>. When executed against an <strong>IQueryable</strong> object, the properties hold the values passed to the <strong>Skip</strong> and <strong>Take</strong> methods. <br />
					<strong>NOTE</strong>: Call the <strong>Skip</strong> and <strong>Take</strong> methods just once in a single query. Otherwise a <strong>NotSupportedException</strong> is thrown and the order in which the,methods are supplied is not taken into account.
				</p>
			</td>
			<td>
				<strong>...GetUsers().Where(..).Skip(skipVal).Take(takeVal).ToList()</strong> <br />
				The query sets the <strong>Skip</strong> and <strong>Take</strong> property of the <strong>PagingArgs</strong> to <strong>skipVal</strong> and <strong>takeVal</strong> correspondingly. If <strong>Skip</strong> or <strong>Take</strong> are not called, these values will be set to <strong>null</strong>.
			</td>
		</tr>
		<tr>
			<td><strong>OrderArgs</strong></td>
			<td>
				This property is responsible for holding the <strong>OrderBy</strong> clause, supplied to the method. It has two properties: 
				<ul>
					<li><strong>MemberName</strong> - the member by which to order</li>
					<li><strong>Direction</strong> - descending or ascending order</li>
				</ul>
			</td>
			<td>
				<strong>...GetUsers().Where(..).Skip(skipVal).Take(takeVal).OrderBy(x => x.Username).ToList()</strong> <br />
				The query sets the <strong>MemberName</strong> to <i>"Username"</i> and <strong>Direction</strong> to <i>"Ascending"</i>. If the query were to hold <strong>OrderByDescending</strong> instead of <strong>OrderBy</strong>, the direction would be set to <i>"Descending"</i>. Once again the order does not matter and only one <strong>OrderBy</strong> query can be executed.
			</td>
		</tr>
		<tr>
			<td><strong>Filters</strong></td>
			<td>
				This property holds the filters specified in the <strong>Where</strong> clause. You can have as many <strong>Where</strong> clauses as you want. The filters are populated in the <strong>Filters</strong> collection. Each filter consists of:
				<ul>
					<li><strong>Member</strong> object holding the MemberName and Action</li>
					<li><strong>Action</strong> - holding the operator</li>
					<li><strong>Value</strong> - holding the compared value</li>
				</ul>
			</td>
			<td>
				The support for the Where clause is limited to two examples only:
				<ul>
					<li><strong>..GetUsers().Where(x => x.Username == "test")</strong></li>
					<li><strong>..GetUsers().Where(x => x.Username == "test" && x.Email == "test@test.com" && ...)</strong></li>
				</ul>
				<div><strong>NOTE:</strong> Only the <strong>&&</strong> clause us supported. Any other clause triggers a <strong>NotSupportedException</strong>.</div>
				<p>
					<div>Multiple <strong>Where</strong> clauses:</div> <br />
					<strong>..GetUsers().Where(x => x.Username == "test").Where(x => x.Email == "test@test.com")</strong>
					<div>In the example above, the Member is mapped to <i>"Username"</i>, Action to <i>"Equals"</i>, and Value to <i>"test"</i>.</div>
				</p>
			</td>
		</tr>
		<tr>
			<td><strong>LastAction</strong></td>
			<td>
				This property represents the method executed most recently on the query. In addition to the methods described in the row above, the <strong>Any</strong> and <strong>Count</strong> methods are also supported. Any filters specified as predicates in the <strong>Count</strong> method are added to the filters collection. The same logic is applied for the method <strong>Any()</strong>. In case the query does not end with the <strong>Any</strong> or <strong>Count</strong> methods, the <strong>LastAction</strong> property is populated with the value <i>"List"</i>.
			</td>
			<td>
				<p>
					<strong>GetUsers().Where(x => x.Username.StartsWith(“test”)).Count()</strong>
					<div>In the example above, <strong>LastAction</strong> is populated with <i>"Count"</i> because the query ends with the <strong>Count()</strong> method.</div>
				</p>
				<p>
					<strong>GetUsers().Where(x => x.Username.StartsWith("test")).Count(x => x.Email.Contains("test@test")) </strong>
					<div>In the example above, the result is an additional filter with a <strong>Member.Name</strong> property with value <i>"Email"</i>, <strong>Action</strong> is mapped to <i>"Contains"</i>, and <strong>Value</strong> to <i>"test@test"</i>.</div>
				</p>
			</td>
		</tr>
		<tr>
			<td><strong>QueryType</strong></td>
			<td>
				This property represents the return type of the query.
			</td>
			<td>
				<p>
					<strong>GetUsers().Where(x => x.Username.StartsWith("test")).Count()</strong><br />
					sets the query type to <strong>IQueryable<User></strong>
				</p>
				<p>
					<strong>GetUsers().Where(x => x.Username.StartsWith("test")).Count()</strong><br />
					sets the query type to <strong>int</strong>
				</p>
				<p>
					<strong>..GetUsers().First()</strong><br />
					sets the query type to <strong>User</strong>
				</p>				
			</td>
		</tr>
	</tbody>
</table>

### Supported queries

<table>
	<thead>
		<tr>
			<td><strong>Query type</strong></td>
			<td><strong>Description</strong></td>
			<td><strong>Query example(s)</strong></td>
		</tr>
	</thead>
	<tbody>
		<tr>
			<td><strong>String Filters</strong></td>
			<td>
				<span>To filter by a string value Sitefinity supports the following string operations:</span>
				<ul>
					<li><strong>Contains</strong></li>
					<li><strong>StartsWith</strong></li>
					<li><strong>EndsWith</strong></li>
					<li><strong>Equals</strong></li>
				</ul>
				<span>The<strong> Member </strong>exposes two sub properties:</span>
				<ul>
					<li><strong>Name</strong> - name of the member which is to be filtered</li>
					<li><strong>Action </strong> - holds the name of the method executed most recently on the property</li>
				</ul>
			</td>
			<td>
				<ul>
					<li><strong>..GetUsers().Where(x => x.Username.StartsWith(“test”))</strong></li>
					<li><strong>..GetUsers().Where(x => x.Username.EndsWith(“test”))</strong></li>
					<li><strong>..GetUsers().Where(x => x.Username.Contains(“test”))</strong></li>
				</ul>
				<div>In the examples above, the filter is populated with <i>"Username"</i> for <strong>Member</strong>, <i>"StartsWith"</i>, <i>"EndsWith"</i>, or <i>"Contains"</i>, respectively, for <strong>Action</strong>, and <i>"test"</i> for <strong>Value</strong>.
				</div>
				<p>
					<div><strong>Member</strong> property:</div>
					<div><strong>..GetUsers().Where(x => x.Username.ToUpper(). Contains (“test”))</strong></div>
					<div>In the example above, <strong>Action</strong> is populated with <i>"ToUpper"</i> to indicate that the user name must be compared with uppercase.</div>
				</p>
			</td>
		</tr>
		<tr>
			<td><strong>Enumerable Filters</strong></td>
			<td>
				<span>Sitefinity also supports the <strong>Enumerable Contains</strong> filter, which means you can compare the value of a property with a predefined list of variables. </span>
			</td>
			<td>
				<p>
					<div><strong>var userIds = new Guid[] { Guid.Parse("5498325D-1F86-4151-848E-77E143A0369C"), Guid.Parse("CFE8F05D-24DB-4756-8209-9EECCE44A1D1") };</strong></div><br />
					<div><strong>..GetUsers().Where(x => userIds. Contains (x.Id))</strong></div><br />
					<div>In the example above, the <strong>Member.Name</strong> property is populated with <i>"Id"</i>, the <strong>Action</strong> is mapped to <i>"Contains"</i>, and <strong>Value</strong> to the <i>"usersIds"</i> collection.</div><br />
				</p>
				<p>
					<div>Using the <strong>Username</strong> property:</div><br />
					<div><strong>var usernames = new string[] { “admin1”, “admin2” };</strong></div><br />
					<div><strong>..GetUsers().Where(x => usernames.Contain(x.Username))</strong></div><br />
				</p>
			</td>
		</tr>
			<td><strong>Additional supported filters</strong></td>
			<td>
				<span>The methods <strong>First</strong> and <strong>FirstOrDefault</strong> are represented using pagination. The same goes for the <strong>Single</strong> or <strong>SingleOrdefault</strong> methods. 
				</span>
			</td>
			<td>
				<p>
					<div>Methods <strong>First</strong> and <strong>FirstOrDefault</strong> identical examples:</div><br />
					<div><strong>GetUsers().Where(x => x.Username.StartsWith(“test”)).FirstOrDefault();</strong></div><br />
					<div><strong>GetUsers().Where(x => x.Username.StartsWith(“test”)).Take(1).ToList();</strong></div><br />
				</p>
				<p>
					<div>Methods <strong>Single</strong> and <strong>SingleOrDefault</strong> identical examples:</div><br />
					<div><strong>GetUsers().Where(x => x.Username.StartsWith(“test”)).SingleOrDefault();</strong></div><br />
					<div><strong>GetUsers().Where(x => x.Username.StartsWith(“test”)).Take(2).</strong></div><br />
				</p>				
				<div>In the examples above, the <strong>Take</strong> parameter is set to <strong>2</strong> because of the way the <strong>Single</strong> method works. The difference here is that if there is more than one match, the <strong>Single</strong> method throws an exception.</div>				
			</td>
		</tr>
	</tbody>
</table>

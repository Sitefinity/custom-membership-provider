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

To read the full documentation, see [Wiki](https://github.com/Sitefinity/custom-membership-provider/wiki).

Custom Membership Provider integration in Sitefinity
==========================
Authentication is an integral part of every web application. In many user scenarios Sitefinity's built in custom membership provider (SitefinityMembershipProvider) would be sufficient in providing the required authentication functionality. However, users might want to integrate their own custom MembershipProvider in order to reuse their current database records with other web applications or are just migrating from standard ASP Applications to Sitefinity and wish to reuse their current authentication store.

Registering such membership providers in the web.config (as in standard ASP applications) will work for small amount of users, but will cause an overhead for large amount ot users (~ 10 000 or above). The reason for this is that the MembershipProvider class provides only a single method for filtering users - the method GetAllUsers(int pageIndex, int pageSize, out int totalRecords), which, as it's signature states, uses only paginated data with no support for filtering, seaching or sorting. Such functionality is not sufficient for the requirements of Sitefinity's user interface which provides functionality for filtering, sorting, searching and paging of user records. 
The main benefits of such functionality are:
* Loading a small amount of users in memory
* Faster user management

So the question here is – How can we fetch filtered, ordered and paged data from our custom membership provider?
With Sitefinity version 8.0, custom MembershipProviders can be configured to support this funcionality using the IBasicQueryExecutor interface.

## IBasicQueryExecutor
The interface exposes a single method - Execute(QueryArgs args). When decorated on a custom MemberipProvider, this method is called whenever data needs to be retireved from the custom MembershipProvider with the arguments for filtering/paging/sorting and search provided in the "args" variable.

## Custom MembershipProvider Example
The example in this repository is a fully functional implementation of the standard SqlMembershipProvider with support for filtering/paging/search and sorting. It is a demonstration on how developers can integrate their custom membership provider to support the user interface of Sitefinity.

## Prerequisities
* A custom membership provider
* Sitefinity 8.0 or above

For the full documentation, please refer to the [Wiki](https://github.com/Sitefinity/custom-membership-provider/wiki).

Custom Membership Provider integration in Sitefinity
==========================
Sitefinity supports out of the box integration with custom membership providers. This allows developers to authenticate their users against custom user stores instead of the default membership provider, that comes with Sitefinity.

The given example is a fully functional implementation of the standard SqlMembershipProvider with support for evaluating basic queryable expressions. The example is used to demonstrate how developers can integrate their custom membership provider in Sitefinity.

# Overview
Throught the code of Sitefinity, there are various queries against the UserManager class that query the Users entity. For example there are queries in the likes of UserManager.GetManager(). GetUsers().Where(x => x.Username == “Martin”).ToList(). This Queryable expression needs to be translated to the membership provider, so that it can fetch only those users which match the filter in the where clause. 

When we look at the MembershipProvider class we can see the method GetAllUsers(int pageIndex, int pageSize, out int totalRecords), which supports fetching paginated data only. Any sorting or filtering is not supported. So the question here is – How can we fetch filtered, ordered and paged data from our custom membership provider?

# Prerequisities
* A custom membership provider
* Sitefinity 8.0 or above

For the full documentation, please refer to the [Wiki](https://github.com/Sitefinity/custom-membership-provider/wiki).

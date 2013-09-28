JPM
===

A simple Json Package Manager

##How to use
This library is meant to be used in a working Application (intended as an executable binary).
To use it just include this library to your project, then to create an instance just do

<pre>
var packageManager = new JPM("C:\\path\\to\\your\\sources.json");
</pre>

where <code>sources.json</code> is just an Array of strings. For example:

<pre>
[
	"http://mysite.com/packages.json"
]
</pre>

Until now it supports multiple sources, and merge all the packages defined in the URLs inside a list.
Then you can use defined methods to refresh packages, install/update or remove a single package.

##FAQ
###Does this works on a WebSite Application (ASP.NET)?
I didn't test it. But it *could* work.

###Will it be forever only for .NET?
Nope, if you like it, i'll port this to other languages too (Java, PHP, Javascript, Python and so on).

###Where can i find the documentation?
Since it's the first day of its life, it's currently a WIP library. When it will be stable, I'll do my best to create a good documentation.

###Err...no comments in the source?
Yeah, sorry. I'll do my best to fix this also.

##License
See LICENSE.
bbdonkey
========

a really simple tool I use to compile a list of followers of bitbucket repositories

Usage
-----

    bbdonkey.exe -h|--help|help|/?
        Prints help

    bbdonkey.exe [<username> [-p|<password> [<owner filter>]]]
        Prints a list of repositories with their followers to the output.

    <username>
        Name of your bitbucket.org user

    -p|<password>
        Your bitbucket.org password. If omitted or if -p is specified, the password will be prompted.

    <owner filter>
        If specified, only repositories with a owner equal to this string will be printed.

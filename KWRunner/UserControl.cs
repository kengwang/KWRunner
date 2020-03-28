using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.DirectoryServices.AccountManagement;
using System.Collections;
using System.Security.Principal;
//C:\Users\yushi\Desktop\Run\KWRunner.exe --user mc5 --jar C:\Users\yushi\Desktop\ --version 1.14.32.1 --serverdir C:\Users\yushi\Desktop\server\
namespace UserControl
{
    public static class Commons
    {
        static Configuration config;
        static AppSettingsSection appSetting;


        public static bool IsAdministrator()
        {
            WindowsIdentity current = WindowsIdentity.GetCurrent();
            WindowsPrincipal windowsPrincipal = new WindowsPrincipal(current);
            return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static bool CreateLocalWindowsAccount(string userName, string passWord, string displayName, string description, string groupName, bool canChangePwd, bool pwdExpires)
        {
            if (!IsAdministrator())
            {
                Console.WriteLine("You are not Administrator!");
                return false;
            }
            bool retIsSuccess = false;
            try
            {
                PrincipalContext context = new PrincipalContext(ContextType.Machine);
                UserPrincipal user = new UserPrincipal(context);
                user.SetPassword(passWord);
                user.DisplayName = displayName;
                user.Name = userName;
                user.Description = description;
                user.UserCannotChangePassword = canChangePwd;
                user.PasswordNeverExpires = pwdExpires;
                user.Save();

                GroupPrincipal group = GroupPrincipal.FindByIdentity(context, groupName);
                group.Members.Add(user);
                group.Save();
                retIsSuccess = true;
            }
            catch (Exception ex)
            {
                retIsSuccess = false;
            }
            return retIsSuccess;
        }

        static GroupPrincipal CreateGroup(string groupName, Boolean isSecurityGroup)
        {
            GroupPrincipal retGroup = null;
            try
            {
                retGroup = IsGroupExist(groupName);
                if (retGroup == null)
                {
                    PrincipalContext ctx = new PrincipalContext(ContextType.Machine);
                    GroupPrincipal insGroupPrincipal = new GroupPrincipal(ctx);
                    insGroupPrincipal.Name = groupName;
                    insGroupPrincipal.IsSecurityGroup = isSecurityGroup;
                    insGroupPrincipal.GroupScope = GroupScope.Local;
                    insGroupPrincipal.Save();
                    retGroup = insGroupPrincipal;
                }
            }
            catch (Exception ex)
            {

            }

            return retGroup;
        }

        static GroupPrincipal IsGroupExist(string groupName)
        {
            GroupPrincipal retGroup = null;
            try
            {
                PrincipalContext ctx = new PrincipalContext(ContextType.Machine);
                GroupPrincipal qbeGroup = new GroupPrincipal(ctx);
                PrincipalSearcher srch = new PrincipalSearcher(qbeGroup);
                foreach (GroupPrincipal ingrp in srch.FindAll())
                {
                    if (ingrp != null && ingrp.Name.Equals(groupName))
                    {
                        retGroup = ingrp;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return retGroup;
        }

        public static int UpdateGroupUsers(string groupName, List<string> usersName)
        {
            List<string> addedUsers = new List<string>();
            int retAddCount = 0;

            GroupPrincipal qbeGroup = CreateGroup(groupName, false);
            foreach (UserPrincipal user in qbeGroup.GetMembers())
            {
                if (usersName.Contains(user.Name))
                {
                    addedUsers.Add(user.Name);
                    retAddCount++;
                }
                else
                {
                    user.Delete();
                }
            }
            foreach (string addedUserName in addedUsers)
            {
                usersName.Remove(addedUserName);
            }
            foreach (string addUserName in usersName)
            {
                bool isSuccess = CreateLocalWindowsAccount(addUserName, "password", addUserName, "", groupName, false, false);
                if (isSuccess) retAddCount++;
            }
            return retAddCount;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CcLibrary.AspNetCore.Attributes;
using CcLibrary.AspNetCore.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace CcLibrary.AspNetCore.Filters {
    public class FilterConfiguration {
        internal IDictionary<Type, ControllerInfo> ControllerInfoDictionary { get; private set; }
        private string dumbLogger;
        internal bool SupportsCustomDataType { get; private set; }
        internal string CustomDataType { get; private set; }

        public FilterConfiguration() {
            ControllerInfoDictionary = new Dictionary<Type, ControllerInfo>();
        }


        public void ScanControllersInfo(Assembly assembly, string customDataType=null) {
            if (assembly == null) {
                throw new ArgumentNullException(nameof(assembly));
            }
            if (!string.IsNullOrEmpty(customDataType)) {
                SupportsCustomDataType = true;
                CustomDataType = customDataType;
            }
            var controllerTypes = assembly.GetTypes().Where(t => t.GetCustomAttribute(typeof(ApiControllerAttribute)) != null);
            Type[] httpMethodTypes = { typeof(HttpGetAttribute), typeof(HttpPostAttribute), typeof(HttpPutAttribute), typeof(HttpPatchAttribute) };
            foreach (var controllerType in controllerTypes) {
                var listOfActions = new List<ControllerAction>();
                foreach (var httpMethodType in httpMethodTypes) {
                    var currentAction = GetAcctionsFromController(controllerType, httpMethodType);
                    if (currentAction!=null) {
                        listOfActions.Add(new ControllerAction(currentAction, httpMethodType));
                    }
                }
                if (listOfActions.Count > 0) {
                    AddControllerInfoToDictionary(listOfActions, controllerType);
                }
            }
        }

        private class ControllerAction {
            public ControllerAction(MemberInfo[] members, Type httpMethodType) {
                Members = members;
                HttpMethodType = httpMethodType;
            }

            public MemberInfo[] Members { get; set; }
            public Type HttpMethodType { get; set; }
        }

        private void AddControllerInfoToDictionary(List<ControllerAction> controllerActions, Type controllerType) {
            var controllerInfo = new ControllerInfo();
            foreach (var action in controllerActions) {
                foreach (var member in action.Members) {
                    string httpMethodName = action.HttpMethodType.Name;
                    var httpMethodAttribute = (HttpMethodAttribute)member.GetCustomAttribute(action.HttpMethodType);
                    var hateoasResourceAttribute = (HateoasResourceAttribute)member.GetCustomAttribute(typeof(HateoasResourceAttribute));
                    controllerInfo.ControllerInfoValues.Add(new ControllerInfoValue(httpMethodAttribute.Name, hateoasResourceAttribute.ResourceType));
                }
            }
            ControllerInfoDictionary.Add(controllerType, controllerInfo);
            dumbLogger += Environment.NewLine;
        }

        private static MemberInfo[] GetAcctionsFromController(Type controllerType, Type attributeType) {
            var members = controllerType.GetMembers(BindingFlags.Public | BindingFlags.Instance)
                .Where(t => { 
                    var attributes = t.GetCustomAttributes();
                    return attributes.Any(attr => attr.GetType().Equals(attributeType))
                        && attributes.Any(attr => attr.GetType().Equals(typeof(HateoasResourceAttribute)));
                }).ToArray();
            return (members?.Count() != 0) ? members : null;
        }

    }
}
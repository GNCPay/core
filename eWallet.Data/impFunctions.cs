using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web;
namespace eWallet.Data
{
    public static class impFunctions
    {
        public static ExpandoObject ToExpando(this DynamicObj anonymousObject)
        {
            //IDictionary<string, object> anonymousDictionary = HtmlHelper.AnonymousObjectToHtmlAttributes(anonymousObject);
            IDictionary<string, object> expando = new ExpandoObject();
            foreach (var item in anonymousObject.dictionary)
                expando.Add(item);

            return (ExpandoObject)expando;
        }
    }
}

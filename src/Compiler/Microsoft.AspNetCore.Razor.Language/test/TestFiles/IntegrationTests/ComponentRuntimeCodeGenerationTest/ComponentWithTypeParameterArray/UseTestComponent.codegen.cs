﻿// <auto-generated/>
#pragma warning disable 1591
namespace Test
{
    #line default
    using global::System;
    using global::System.Collections.Generic;
    using global::System.Linq;
    using global::System.Threading.Tasks;
    using global::Microsoft.AspNetCore.Components;
#nullable restore
#line (1,2)-(2,1) "x:\dir\subdir\Test\UseTestComponent.cshtml"
using Test

#line default
#line hidden
#nullable disable
    ;
    #nullable restore
    public partial class UseTestComponent : global::Microsoft.AspNetCore.Components.ComponentBase
    #nullable disable
    {
        #pragma warning disable 1998
        protected override void BuildRenderTree(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
        {
            global::__Blazor.Test.UseTestComponent.TypeInference.CreateTestComponent_0(__builder, 0, 1, 
#nullable restore
#line (2,23)-(2,29) "x:\dir\subdir\Test\UseTestComponent.cshtml"
items1

#line default
#line hidden
#nullable disable
            , 2, 
#nullable restore
#line (2,37)-(2,43) "x:\dir\subdir\Test\UseTestComponent.cshtml"
items2

#line default
#line hidden
#nullable disable
            , 3, 
#nullable restore
#line (2,51)-(2,57) "x:\dir\subdir\Test\UseTestComponent.cshtml"
items3

#line default
#line hidden
#nullable disable
            , 4, (context) => (__builder2) => {
                __builder2.OpenElement(5, "p");
                __builder2.AddContent(6, 
#nullable restore
#line (3,9)-(3,31) "x:\dir\subdir\Test\UseTestComponent.cshtml"
context[0].description

#line default
#line hidden
#nullable disable
                );
                __builder2.CloseElement();
            }
            );
        }
        #pragma warning restore 1998
#nullable restore
#line (6,8)-(11,1) "x:\dir\subdir\Test\UseTestComponent.cshtml"

    static Tag tag = new Tag() { description = "A description."};
    Tag[] items1 = new [] { tag };
    List<Tag[]> items2 = new List<Tag[]>() { new [] { tag } };
    Tag[] items3() => new [] { tag };

#line default
#line hidden
#nullable disable

    }
}
namespace __Blazor.Test.UseTestComponent
{
    #line hidden
    internal static class TypeInference
    {
        public static void CreateTestComponent_0<TItem>(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder, int seq, int __seq0, TItem[] __arg0, int __seq1, global::System.Collections.Generic.List<TItem[]> __arg1, int __seq2, global::System.Func<TItem[]> __arg2, int __seq3, global::Microsoft.AspNetCore.Components.RenderFragment<TItem[]> __arg3)
        {
        __builder.OpenComponent<global::Test.TestComponent<TItem>>(seq);
        __builder.AddComponentParameter(__seq0, "Items1", __arg0);
        __builder.AddComponentParameter(__seq1, "Items2", __arg1);
        __builder.AddComponentParameter(__seq2, "Items3", __arg2);
        __builder.AddComponentParameter(__seq3, "ChildContent", __arg3);
        __builder.CloseComponent();
        }
    }
}
#pragma warning restore 1591

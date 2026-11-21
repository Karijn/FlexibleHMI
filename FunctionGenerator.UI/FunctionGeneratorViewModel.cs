using FlexibleHMI.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media;

namespace FunctionGenerator.UI;

public class FunctionGeneratorViewModel : TreeItemNode
{
    public FunctionGeneratorViewModel()
    {
        Name = "Function Generator";
        Description = "";
        Image = TreeItemOption.CreateDiamondImage(Colors.Olive);
    }

    public override string Kind => "FunctionGenerator";

    public override int MaxChildren => -1;

    public override string ChildRule => "";

    public override bool CanAcceptChildren => true;

    public override TreeItemNode Copy()
    {
        TreeItemNode node = Copy(new FunctionGeneratorViewModel());

        return node;
    }
}

public class FunctionGeneratorCreator : TreeItemOption
{
    public FunctionGeneratorCreator(IServiceProvider serviceProvider)
        : base("functionGenerator", "FunctionGenerator", "Function Generation", "Can contain children", TreeItemOption.CreateDiamondImage(Colors.Red), serviceProvider)
    { }
}

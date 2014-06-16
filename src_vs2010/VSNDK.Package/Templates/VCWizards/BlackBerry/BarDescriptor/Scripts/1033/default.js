function OnPrep(selProj, selObj)
{
	var L_WizardDialogTitle_Text = "BlackBerry Descriptor Wizard";
	return PrepCodeWizard(selProj, L_WizardDialogTitle_Text);
}

function GetTargetName(strName, strProjectName, strResPath, strHelpPath)
{
	try
	{
		return "bar-descriptor.xml";
	}
	catch(e)
	{
		throw e;
	}
}

function OnFinish(selProj, selObj)
{   
	var oCM;
	try
	{
		oCM	= selProj.CodeModel;

		var strTemplatePath = wizard.FindSymbol("TEMPLATES_PATH");
		var strFileName = "bar-descriptor.xml";
		
		if (wizard.DoesFileExist(strFileName))
		{
			wizard.ReportError(strFileName + " already exists.");
			return VS_E_WIZCANCEL;
		}
		
		var L_TRANSACTION_Text = "Add ";
		oCM.StartTransaction(L_TRANSACTION_Text + strFileName);
		
		wizard.AddSymbol("PROJECT_NAME", wizard.FindSymbol("PROJECT_NAME"));
		
		// render the srf file 
		wizard.RenderTemplate(strTemplatePath + "\\" + "bar-descriptor.xml", strFileName);
		
		// add the srf file to the selected folder (could be the root of the project)
		var srffile = selObj.AddFromFile(strFileName);
        if( srffile )
        {
            var window = srffile.Open(vsViewKindPrimary);
            if(window)
                window.visible = true;
        }

		oCM.CommitTransaction();
	}
  	catch(e)
	{
		if (oCM)
			oCM.AbortTransaction();

		if (e.description.length != 0)
			SetErrorInfo(e);
		return e.number
	}
}


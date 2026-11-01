using RuntimeInspectorNamespace;
using System;

[Serializable, ExpandArray]
public class TaskTrueFalseVariant : TaskBaseVariant, IValidateContent
{
    [InfoString("True or False")] public bool correctAnswer;
}

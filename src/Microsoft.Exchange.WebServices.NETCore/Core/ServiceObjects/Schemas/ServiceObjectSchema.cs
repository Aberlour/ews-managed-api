/*
 * Exchange Web Services Managed API
 *
 * Copyright (c) Microsoft Corporation
 * All rights reserved.
 *
 * MIT License
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this
 * software and associated documentation files (the "Software"), to deal in the Software
 * without restriction, including without limitation the rights to use, copy, modify, merge,
 * publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
 * to whom the Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or
 * substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
 * PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
 * FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 */

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Microsoft.Exchange.WebServices.Data;

using PropertyDefinitionDictionary = LazyMember<Dictionary<string, PropertyDefinitionBase>>;
using SchemaTypeList = LazyMember<List<Type>>;

/// <summary>
///     Represents the base class for all item and folder schemas.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class ServiceObjectSchema : IEnumerable<PropertyDefinition>
{
    private static readonly object lockObject = new object();

    /// <summary>
    ///     List of all schema types.
    /// </summary>
    /// <remarks>
    ///     If you add a new ServiceObject subclass that has an associated schema, add the schema type
    ///     to the list below.
    /// </remarks>
    private static readonly SchemaTypeList allSchemaTypes = new SchemaTypeList(
        delegate
        {
            var typeList = new List<Type>();
            typeList.Add(typeof(AppointmentSchema));
            typeList.Add(typeof(CalendarResponseObjectSchema));
            typeList.Add(typeof(CancelMeetingMessageSchema));
            typeList.Add(typeof(ContactGroupSchema));
            typeList.Add(typeof(ContactSchema));
            typeList.Add(typeof(ConversationSchema));
            typeList.Add(typeof(EmailMessageSchema));
            typeList.Add(typeof(FolderSchema));
            typeList.Add(typeof(ItemSchema));
            typeList.Add(typeof(MeetingMessageSchema));
            typeList.Add(typeof(MeetingRequestSchema));
            typeList.Add(typeof(MeetingCancellationSchema));
            typeList.Add(typeof(MeetingResponseSchema));
            typeList.Add(typeof(PersonaSchema));
            typeList.Add(typeof(PostItemSchema));
            typeList.Add(typeof(PostReplySchema));
            typeList.Add(typeof(ResponseMessageSchema));
            typeList.Add(typeof(ResponseObjectSchema));
            typeList.Add(typeof(ServiceObjectSchema));
            typeList.Add(typeof(SearchFolderSchema));
            typeList.Add(typeof(TaskSchema));

#if DEBUG
            // Verify that all Schema types in the Managed API assembly have been included.
            var missingTypes = from type in typeof(ServiceObjectSchema).GetTypeInfo().Assembly.ExportedTypes
                where type.GetTypeInfo().IsSubclassOf(typeof(ServiceObjectSchema)) && !typeList.Contains(type)
                select type;
            if (missingTypes.Count() > 0)
            {
                throw new ServiceLocalException("SchemaTypeList does not include all defined schema types.");
            }
#endif

            return typeList;
        }
    );

    /// <summary>
    ///     Dictionary of all property definitions.
    /// </summary>
    private static readonly PropertyDefinitionDictionary allSchemaProperties = new PropertyDefinitionDictionary(
        delegate
        {
            var propDefDictionary = new Dictionary<string, PropertyDefinitionBase>();
            foreach (var type in allSchemaTypes.Member)
            {
                AddSchemaPropertiesToDictionary(type, propDefDictionary);
            }

            return propDefDictionary;
        }
    );

    /// <summary>
    ///     Delegate that takes a property definition and matching static field info.
    /// </summary>
    /// <param name="propertyDefinition">Property definition.</param>
    /// <param name="fieldInfo">Field info.</param>
    internal delegate void PropertyFieldInfoDelegate(PropertyDefinition propertyDefinition, FieldInfo fieldInfo);

    /// <summary>
    ///     Call delegate for each public static PropertyDefinition field in type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="propFieldDelegate">The property field delegate.</param>
    internal static void ForeachPublicStaticPropertyFieldInType(Type type, PropertyFieldInfoDelegate propFieldDelegate)
    {
        var fieldInfos = type.GetRuntimeFields().ToArray();

        foreach (var fieldInfo in fieldInfos)
        {
            if (fieldInfo.FieldType == typeof(PropertyDefinition) ||
                fieldInfo.FieldType.GetTypeInfo().IsSubclassOf(typeof(PropertyDefinition)))
            {
                var propertyDefinition = (PropertyDefinition)fieldInfo.GetValue(null);
                propFieldDelegate(propertyDefinition, fieldInfo);
            }
        }
    }

    /// <summary>
    ///     Adds schema properties to dictionary.
    /// </summary>
    /// <param name="type">Schema type.</param>
    /// <param name="propDefDictionary">The property definition dictionary.</param>
    internal static void AddSchemaPropertiesToDictionary(
        Type type,
        Dictionary<string, PropertyDefinitionBase> propDefDictionary
    )
    {
        ForeachPublicStaticPropertyFieldInType(
            type,
            delegate(PropertyDefinition propertyDefinition, FieldInfo fieldInfo)
            {
                // Some property definitions descend from ServiceObjectPropertyDefinition but don't have
                // a Uri, like ExtendedProperties. Ignore them.
                if (!string.IsNullOrEmpty(propertyDefinition.Uri))
                {
                    PropertyDefinitionBase existingPropertyDefinition;
                    if (propDefDictionary.TryGetValue(propertyDefinition.Uri, out existingPropertyDefinition))
                    {
                        EwsUtilities.Assert(
                            existingPropertyDefinition == propertyDefinition,
                            "Schema.allSchemaProperties.delegate",
                            string.Format(
                                "There are at least two distinct property definitions with the following URI: {0}",
                                propertyDefinition.Uri
                            )
                        );
                    }
                    else
                    {
                        propDefDictionary.Add(propertyDefinition.Uri, propertyDefinition);

                        // The following is a "generic hack" to register properties that are not public and
                        // thus not returned by the above GetFields call. It is currently solely used to register
                        // the MeetingTimeZone property.
                        var associatedInternalProperties = propertyDefinition.GetAssociatedInternalProperties();

                        foreach (var associatedInternalProperty in associatedInternalProperties)
                        {
                            propDefDictionary.Add(associatedInternalProperty.Uri, associatedInternalProperty);
                        }
                    }
                }
            }
        );
    }

    /// <summary>
    ///     Adds the schema property names to dictionary.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="propertyNameDictionary">The property name dictionary.</param>
    private static void AddSchemaPropertyNamesToDictionary(
        Type type,
        Dictionary<PropertyDefinition, string> propertyNameDictionary
    )
    {
        ForeachPublicStaticPropertyFieldInType(
            type,
            delegate(PropertyDefinition propertyDefinition, FieldInfo fieldInfo)
            {
                propertyNameDictionary.Add(propertyDefinition, fieldInfo.Name);
            }
        );
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ServiceObjectSchema" /> class.
    /// </summary>
    internal ServiceObjectSchema()
    {
        RegisterProperties();
    }

    /// <summary>
    ///     Finds the property definition.
    /// </summary>
    /// <param name="uri">The URI.</param>
    /// <returns>Property definition.</returns>
    internal static PropertyDefinitionBase FindPropertyDefinition(string uri)
    {
        return allSchemaProperties.Member[uri];
    }

    /// <summary>
    ///     Initialize schema property names.
    /// </summary>
    internal static void InitializeSchemaPropertyNames()
    {
        lock (lockObject)
        {
            foreach (var type in allSchemaTypes.Member)
            {
                ForeachPublicStaticPropertyFieldInType(
                    type,
                    delegate(PropertyDefinition propDef, FieldInfo fieldInfo) { propDef.Name = fieldInfo.Name; }
                );
            }
        }
    }

    /// <summary>
    ///     Defines the ExtendedProperties property.
    /// </summary>
    [SuppressMessage(
        "Microsoft.Security",
        "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes",
        Justification = "Immutable type"
    )]
    public static readonly PropertyDefinition ExtendedProperties =
        new ComplexPropertyDefinition<ExtendedPropertyCollection>(
            XmlElementNames.ExtendedProperty,
            PropertyDefinitionFlags.AutoInstantiateOnRead |
            PropertyDefinitionFlags.ReuseInstance |
            PropertyDefinitionFlags.CanSet |
            PropertyDefinitionFlags.CanUpdate,
            ExchangeVersion.Exchange2007_SP1,
            delegate { return new ExtendedPropertyCollection(); }
        );

    private readonly Dictionary<string, PropertyDefinition> properties = new Dictionary<string, PropertyDefinition>();
    private readonly List<PropertyDefinition> visibleProperties = new List<PropertyDefinition>();
    private readonly List<PropertyDefinition> firstClassProperties = new List<PropertyDefinition>();
    private readonly List<PropertyDefinition> firstClassSummaryProperties = new List<PropertyDefinition>();
    private readonly List<IndexedPropertyDefinition> indexedProperties = new List<IndexedPropertyDefinition>();

    /// <summary>
    ///     Registers a schema property.
    /// </summary>
    /// <param name="property">The property to register.</param>
    /// <param name="isInternal">Indicates whether the property is internal or should be visible to developers.</param>
    private void RegisterProperty(PropertyDefinition property, bool isInternal)
    {
        properties.Add(property.XmlElementName, property);

        if (!isInternal)
        {
            visibleProperties.Add(property);
        }

        // If this property does not have to be requested explicitly, add
        // it to the list of firstClassProperties.
        if (!property.HasFlag(PropertyDefinitionFlags.MustBeExplicitlyLoaded))
        {
            firstClassProperties.Add(property);
        }

        // If this property can be found, add it to the list of firstClassSummaryProperties
        if (property.HasFlag(PropertyDefinitionFlags.CanFind))
        {
            firstClassSummaryProperties.Add(property);
        }
    }

    /// <summary>
    ///     Registers a schema property that will be visible to developers.
    /// </summary>
    /// <param name="property">The property to register.</param>
    internal void RegisterProperty(PropertyDefinition property)
    {
        RegisterProperty(property, false);
    }

    /// <summary>
    ///     Registers an internal schema property.
    /// </summary>
    /// <param name="property">The property to register.</param>
    internal void RegisterInternalProperty(PropertyDefinition property)
    {
        RegisterProperty(property, true);
    }

    /// <summary>
    ///     Registers an indexed property.
    /// </summary>
    /// <param name="indexedProperty">The indexed property to register.</param>
    internal void RegisterIndexedProperty(IndexedPropertyDefinition indexedProperty)
    {
        indexedProperties.Add(indexedProperty);
    }

    /// <summary>
    ///     Registers properties.
    /// </summary>
    internal virtual void RegisterProperties()
    {
    }

    /// <summary>
    ///     Gets the list of first class properties for this service object type.
    /// </summary>
    internal List<PropertyDefinition> FirstClassProperties => firstClassProperties;

    /// <summary>
    ///     Gets the list of first class summary properties for this service object type.
    /// </summary>
    internal List<PropertyDefinition> FirstClassSummaryProperties => firstClassSummaryProperties;

    /// <summary>
    ///     Gets the list of indexed properties for this service object type.
    /// </summary>
    internal List<IndexedPropertyDefinition> IndexedProperties => indexedProperties;

    /// <summary>
    ///     Tries to get property definition.
    /// </summary>
    /// <param name="xmlElementName">Name of the XML element.</param>
    /// <param name="propertyDefinition">The property definition.</param>
    /// <returns>True if property definition exists.</returns>
    internal bool TryGetPropertyDefinition(string xmlElementName, out PropertyDefinition propertyDefinition)
    {
        return properties.TryGetValue(xmlElementName, out propertyDefinition);
    }


    #region IEnumerable<SimplePropertyDefinition> Members

    /// <summary>
    ///     Obtains an enumerator for the properties of the schema.
    /// </summary>
    /// <returns>An IEnumerator instance.</returns>
    public IEnumerator<PropertyDefinition> GetEnumerator()
    {
        return visibleProperties.GetEnumerator();
    }

    #endregion


    #region IEnumerable Members

    /// <summary>
    ///     Obtains an enumerator for the properties of the schema.
    /// </summary>
    /// <returns>An IEnumerator instance.</returns>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return visibleProperties.GetEnumerator();
    }

    #endregion
}

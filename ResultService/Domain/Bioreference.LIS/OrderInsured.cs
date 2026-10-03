using System;
using System.Data;
using Bioreference.Common;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    [Serializable()]
    public class OrderInsured : Person
    {


        #region Private/Protected Members

        private Relationship m_relation = Relationship.Unknown;
        private OrderPatientInsurance m_parent = null;

        #endregion

        #region Constructor

        internal OrderInsured(OrderPatientInsurance parent)
        {
            m_parent = parent;
            FlagChild();
        }

        #endregion

        #region Public Properties

        public override object IdentifierId
        {
            get
            {
                return m_id;
            }
        }

        public OrderPatientInsurance Parent
        {
            get
            {
                return m_parent;
            }
        }

        /// <summary>
    /// OrderInsured's relationship to the OrderPatient.
    /// </summary>
    /// <value></value>
    /// <returns></returns>
    /// <remarks></remarks>
        public Relationship Relation
        {
            get
            {
                return m_relation;
            }
            set
            {
                m_relation = value;
            }
        }

        #endregion

        #region Public Functions

        // Public Sub Delete()

        // DataFactory.Delete(New Criteria(Me.m_id))

        // End Sub

        #endregion

        #region Data Functions

        internal new void Load(DataRow row)
        {

            m_relation = (Relationship)Conversions.ToInteger(row["RelationType"]);

            base.Load(row);

            FlagClean();

        }

        #endregion

        #region Internal Criteria Class

        // <Serializable()> _
        // Friend Class Criteria

        // Private m_id As Integer

        // Friend Sub New(ByVal insuredId As Integer)
        // m_id = insuredId
        // End Sub

        // Friend ReadOnly Property GuarantorID()
        // Get
        // Return m_id
        // End Get
        // End Property

        // End Class

        #endregion

    }
}
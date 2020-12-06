/*
 Licensed under the Apache License, Version 2.0

 http://www.apache.org/licenses/LICENSE-2.0
 */

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Xml2CSharp
{
    [XmlRoot(ElementName = "m_ID")]
    public class M_ID
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }

        [XmlAttribute(AttributeName = "valuetype")]
        public string Valuetype { get; set; }
    }

    [XmlRoot(ElementName = "m_iGenerateID")]
    public class M_iGenerateID
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }

        [XmlAttribute(AttributeName = "valuetype")]
        public string Valuetype { get; set; }
    }

    [XmlRoot(ElementName = "iLayerBitmask")]
    public class ILayerBitmask
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }

        [XmlAttribute(AttributeName = "valuetype")]
        public string Valuetype { get; set; }
    }

    [XmlRoot(ElementName = "m_vPosTopLeft")]
    public class M_vPosTopLeft
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }

        [XmlAttribute(AttributeName = "valuetype")]
        public string Valuetype { get; set; }
    }

    [XmlRoot(ElementName = "m_vPosBottomRight")]
    public class M_vPosBottomRight
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }

        [XmlAttribute(AttributeName = "valuetype")]
        public string Valuetype { get; set; }
    }

    [XmlRoot(ElementName = "m_fRotation")]
    public class M_fRotation
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }

        [XmlAttribute(AttributeName = "valuetype")]
        public string Valuetype { get; set; }
    }

    [XmlRoot(ElementName = "m_vSize")]
    public class M_vSize
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }

        [XmlAttribute(AttributeName = "valuetype")]
        public string Valuetype { get; set; }
    }

    [XmlRoot(ElementName = "m_bShowCustomEntity")]
    public class M_bShowCustomEntity
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }

        [XmlAttribute(AttributeName = "valuetype")]
        public string Valuetype { get; set; }
    }

    [XmlRoot(ElementName = "m_ColorLDR")]
    public class M_ColorLDR
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }

        [XmlAttribute(AttributeName = "valuetype")]
        public string Valuetype { get; set; }
    }

    [XmlRoot(ElementName = "m_eSpawnType")]
    public class M_eSpawnType
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "VStartEventBox")]
    public class VStartEventBox
    {
        [XmlElement(ElementName = "m_ID")]
        public M_ID M_ID { get; set; }

        [XmlElement(ElementName = "m_iGenerateID")]
        public M_iGenerateID M_iGenerateID { get; set; }

        [XmlElement(ElementName = "iLayerBitmask")]
        public ILayerBitmask ILayerBitmask { get; set; }

        [XmlElement(ElementName = "m_vPosTopLeft")]
        public M_vPosTopLeft M_vPosTopLeft { get; set; }

        [XmlElement(ElementName = "m_vPosBottomRight")]
        public M_vPosBottomRight M_vPosBottomRight { get; set; }

        [XmlElement(ElementName = "m_fRotation")]
        public M_fRotation M_fRotation { get; set; }

        [XmlElement(ElementName = "m_vSize")]
        public M_vSize M_vSize { get; set; }

        [XmlElement(ElementName = "m_bShowCustomEntity")]
        public M_bShowCustomEntity M_bShowCustomEntity { get; set; }

        [XmlElement(ElementName = "m_ColorLDR")]
        public M_ColorLDR M_ColorLDR { get; set; }

        [XmlElement(ElementName = "m_eSpawnType")]
        public M_eSpawnType M_eSpawnType { get; set; }

        [XmlAttribute(AttributeName = "boxtype")]
        public string Boxtype { get; set; }
    }

    [XmlRoot(ElementName = "m_iParentPortalBoxID")]
    public class M_iParentPortalBoxID
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "VPortalExitBox")]
    public class VPortalExitBox
    {
        [XmlElement(ElementName = "m_ID")]
        public M_ID M_ID { get; set; }

        [XmlElement(ElementName = "m_iGenerateID")]
        public M_iGenerateID M_iGenerateID { get; set; }

        [XmlElement(ElementName = "iLayerBitmask")]
        public ILayerBitmask ILayerBitmask { get; set; }

        [XmlElement(ElementName = "m_vPosTopLeft")]
        public M_vPosTopLeft M_vPosTopLeft { get; set; }

        [XmlElement(ElementName = "m_vPosBottomRight")]
        public M_vPosBottomRight M_vPosBottomRight { get; set; }

        [XmlElement(ElementName = "m_fRotation")]
        public M_fRotation M_fRotation { get; set; }

        [XmlElement(ElementName = "m_vSize")]
        public M_vSize M_vSize { get; set; }

        [XmlElement(ElementName = "m_bShowCustomEntity")]
        public M_bShowCustomEntity M_bShowCustomEntity { get; set; }

        [XmlElement(ElementName = "m_ColorLDR")]
        public M_ColorLDR M_ColorLDR { get; set; }

        [XmlElement(ElementName = "m_iParentPortalBoxID")]
        public M_iParentPortalBoxID M_iParentPortalBoxID { get; set; }

        [XmlAttribute(AttributeName = "boxtype")]
        public string Boxtype { get; set; }
    }

    [XmlRoot(ElementName = "m_iField")]
    public class M_iField
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iEventObject")]
    public class M_iEventObject
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "VMazeEscapeBox")]
    public class VMazeEscapeBox
    {
        [XmlElement(ElementName = "m_ID")]
        public M_ID M_ID { get; set; }

        [XmlElement(ElementName = "m_iGenerateID")]
        public M_iGenerateID M_iGenerateID { get; set; }

        [XmlElement(ElementName = "iLayerBitmask")]
        public ILayerBitmask ILayerBitmask { get; set; }

        [XmlElement(ElementName = "m_vPosTopLeft")]
        public M_vPosTopLeft M_vPosTopLeft { get; set; }

        [XmlElement(ElementName = "m_vPosBottomRight")]
        public M_vPosBottomRight M_vPosBottomRight { get; set; }

        [XmlElement(ElementName = "m_fRotation")]
        public M_fRotation M_fRotation { get; set; }

        [XmlElement(ElementName = "m_vSize")]
        public M_vSize M_vSize { get; set; }

        [XmlElement(ElementName = "m_bShowCustomEntity")]
        public M_bShowCustomEntity M_bShowCustomEntity { get; set; }

        [XmlElement(ElementName = "m_ColorLDR")]
        public M_ColorLDR M_ColorLDR { get; set; }

        [XmlElement(ElementName = "m_iField")]
        public M_iField M_iField { get; set; }

        [XmlElement(ElementName = "m_iEventObject")]
        public M_iEventObject M_iEventObject { get; set; }

        [XmlAttribute(AttributeName = "boxtype")]
        public string Boxtype { get; set; }
    }

    [XmlRoot(ElementName = "m_bShowGUI")]
    public class M_bShowGUI
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iGUI")]
    public class M_iGUI
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_eJumpType")]
    public class M_eJumpType
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iJumpMap")]
    public class M_iJumpMap
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iJump")]
    public class M_iJump
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_eEffectType")]
    public class M_eEffectType
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_szDisableEffect")]
    public class M_szDisableEffect
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_szEnableEffect")]
    public class M_szEnableEffect
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iUIString")]
    public class M_iUIString
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iNextSectorID")]
    public class M_iNextSectorID
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_bCallScript")]
    public class M_bCallScript
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_uiOpenEpisode")]
    public class M_uiOpenEpisode
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_uiCompleteEpisode")]
    public class M_uiCompleteEpisode
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_fStringOffsetX")]
    public class M_fStringOffsetX
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_fStringOffsetY")]
    public class M_fStringOffsetY
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_fStringOffsetZ")]
    public class M_fStringOffsetZ
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iClearSectorID1")]
    public class M_iClearSectorID1
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iClearSectorID2")]
    public class M_iClearSectorID2
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iClearSectorID3")]
    public class M_iClearSectorID3
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iClearSectorID4")]
    public class M_iClearSectorID4
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iClearSectorID5")]
    public class M_iClearSectorID5
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_fClearSectorChance1")]
    public class M_fClearSectorChance1
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_fClearSectorChance2")]
    public class M_fClearSectorChance2
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_fClearSectorChance3")]
    public class M_fClearSectorChance3
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_fClearSectorChance4")]
    public class M_fClearSectorChance4
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_fClearSectorChance5")]
    public class M_fClearSectorChance5
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iMaxUserCount")]
    public class M_iMaxUserCount
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iMaxTimeCount")]
    public class M_iMaxTimeCount
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "VPortalBox")]
    public class VPortalBox
    {
        [XmlElement(ElementName = "m_ID")]
        public M_ID M_ID { get; set; }

        [XmlElement(ElementName = "m_iGenerateID")]
        public M_iGenerateID M_iGenerateID { get; set; }

        [XmlElement(ElementName = "iLayerBitmask")]
        public ILayerBitmask ILayerBitmask { get; set; }

        [XmlElement(ElementName = "m_vPosTopLeft")]
        public M_vPosTopLeft M_vPosTopLeft { get; set; }

        [XmlElement(ElementName = "m_vPosBottomRight")]
        public M_vPosBottomRight M_vPosBottomRight { get; set; }

        [XmlElement(ElementName = "m_fRotation")]
        public M_fRotation M_fRotation { get; set; }

        [XmlElement(ElementName = "m_vSize")]
        public M_vSize M_vSize { get; set; }

        [XmlElement(ElementName = "m_bShowCustomEntity")]
        public M_bShowCustomEntity M_bShowCustomEntity { get; set; }

        [XmlElement(ElementName = "m_ColorLDR")]
        public M_ColorLDR M_ColorLDR { get; set; }

        [XmlElement(ElementName = "m_bShowGUI")]
        public M_bShowGUI M_bShowGUI { get; set; }

        [XmlElement(ElementName = "m_iGUI")]
        public M_iGUI M_iGUI { get; set; }

        [XmlElement(ElementName = "m_eJumpType")]
        public M_eJumpType M_eJumpType { get; set; }

        [XmlElement(ElementName = "m_iJumpMap")]
        public M_iJumpMap M_iJumpMap { get; set; }

        [XmlElement(ElementName = "m_iJump")]
        public M_iJump M_iJump { get; set; }

        [XmlElement(ElementName = "m_eEffectType")]
        public M_eEffectType M_eEffectType { get; set; }

        [XmlElement(ElementName = "m_szDisableEffect")]
        public M_szDisableEffect M_szDisableEffect { get; set; }

        [XmlElement(ElementName = "m_szEnableEffect")]
        public M_szEnableEffect M_szEnableEffect { get; set; }

        [XmlElement(ElementName = "m_iUIString")]
        public M_iUIString M_iUIString { get; set; }

        [XmlElement(ElementName = "m_iNextSectorID")]
        public M_iNextSectorID M_iNextSectorID { get; set; }

        [XmlElement(ElementName = "m_bCallScript")]
        public M_bCallScript M_bCallScript { get; set; }

        [XmlElement(ElementName = "m_uiOpenEpisode")]
        public M_uiOpenEpisode M_uiOpenEpisode { get; set; }

        [XmlElement(ElementName = "m_uiCompleteEpisode")]
        public M_uiCompleteEpisode M_uiCompleteEpisode { get; set; }

        [XmlElement(ElementName = "m_fStringOffsetX")]
        public M_fStringOffsetX M_fStringOffsetX { get; set; }

        [XmlElement(ElementName = "m_fStringOffsetY")]
        public M_fStringOffsetY M_fStringOffsetY { get; set; }

        [XmlElement(ElementName = "m_fStringOffsetZ")]
        public M_fStringOffsetZ M_fStringOffsetZ { get; set; }

        [XmlElement(ElementName = "m_iClearSectorID1")]
        public M_iClearSectorID1 M_iClearSectorID1 { get; set; }

        [XmlElement(ElementName = "m_iClearSectorID2")]
        public M_iClearSectorID2 M_iClearSectorID2 { get; set; }

        [XmlElement(ElementName = "m_iClearSectorID3")]
        public M_iClearSectorID3 M_iClearSectorID3 { get; set; }

        [XmlElement(ElementName = "m_iClearSectorID4")]
        public M_iClearSectorID4 M_iClearSectorID4 { get; set; }

        [XmlElement(ElementName = "m_iClearSectorID5")]
        public M_iClearSectorID5 M_iClearSectorID5 { get; set; }

        [XmlElement(ElementName = "m_fClearSectorChance1")]
        public M_fClearSectorChance1 M_fClearSectorChance1 { get; set; }

        [XmlElement(ElementName = "m_fClearSectorChance2")]
        public M_fClearSectorChance2 M_fClearSectorChance2 { get; set; }

        [XmlElement(ElementName = "m_fClearSectorChance3")]
        public M_fClearSectorChance3 M_fClearSectorChance3 { get; set; }

        [XmlElement(ElementName = "m_fClearSectorChance4")]
        public M_fClearSectorChance4 M_fClearSectorChance4 { get; set; }

        [XmlElement(ElementName = "m_fClearSectorChance5")]
        public M_fClearSectorChance5 M_fClearSectorChance5 { get; set; }

        [XmlElement(ElementName = "m_iMaxUserCount")]
        public M_iMaxUserCount M_iMaxUserCount { get; set; }

        [XmlElement(ElementName = "m_iMaxTimeCount")]
        public M_iMaxTimeCount M_iMaxTimeCount { get; set; }

        [XmlAttribute(AttributeName = "boxtype")]
        public string Boxtype { get; set; }
    }

    [XmlRoot(ElementName = "m_eType")]
    public class M_eType
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }

        [XmlAttribute(AttributeName = "valuetype")]
        public string Valuetype { get; set; }
    }

    [XmlRoot(ElementName = "m_szEnterLua")]
    public class M_szEnterLua
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_eClearType")]
    public class M_eClearType
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iClearKillRatio")]
    public class M_iClearKillRatio
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_szClearLua")]
    public class M_szClearLua
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iSectorTitle")]
    public class M_iSectorTitle
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iSectorExitType")]
    public class M_iSectorExitType
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iSectorExitID")]
    public class M_iSectorExitID
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iRelativeSectorID")]
    public class M_iRelativeSectorID
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iConditionKillRatio_1Step")]
    public class M_iConditionKillRatio_1Step
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iConditionKillRatio_2Step")]
    public class M_iConditionKillRatio_2Step
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iConditionKillRatio_3Step")]
    public class M_iConditionKillRatio_3Step
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iConditionKillRatio_4Step")]
    public class M_iConditionKillRatio_4Step
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iConditionKillRatio_5Step")]
    public class M_iConditionKillRatio_5Step
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "VSectorBox")]
    public class VSectorBox
    {
        [XmlElement(ElementName = "m_ID")]
        public M_ID M_ID { get; set; }

        [XmlElement(ElementName = "m_iGenerateID")]
        public M_iGenerateID M_iGenerateID { get; set; }

        [XmlElement(ElementName = "iLayerBitmask")]
        public ILayerBitmask ILayerBitmask { get; set; }

        [XmlElement(ElementName = "m_vPosTopLeft")]
        public M_vPosTopLeft M_vPosTopLeft { get; set; }

        [XmlElement(ElementName = "m_vPosBottomRight")]
        public M_vPosBottomRight M_vPosBottomRight { get; set; }

        [XmlElement(ElementName = "m_fRotation")]
        public M_fRotation M_fRotation { get; set; }

        [XmlElement(ElementName = "m_vSize")]
        public M_vSize M_vSize { get; set; }

        [XmlElement(ElementName = "m_bShowCustomEntity")]
        public M_bShowCustomEntity M_bShowCustomEntity { get; set; }

        [XmlElement(ElementName = "m_ColorLDR")]
        public M_ColorLDR M_ColorLDR { get; set; }

        [XmlElement(ElementName = "m_eType")]
        public M_eType M_eType { get; set; }

        [XmlElement(ElementName = "m_szEnterLua")]
        public M_szEnterLua M_szEnterLua { get; set; }

        [XmlElement(ElementName = "m_eClearType")]
        public M_eClearType M_eClearType { get; set; }

        [XmlElement(ElementName = "m_iClearKillRatio")]
        public M_iClearKillRatio M_iClearKillRatio { get; set; }

        [XmlElement(ElementName = "m_szClearLua")]
        public M_szClearLua M_szClearLua { get; set; }

        [XmlElement(ElementName = "m_iSectorTitle")]
        public M_iSectorTitle M_iSectorTitle { get; set; }

        [XmlElement(ElementName = "m_iSectorExitType")]
        public M_iSectorExitType M_iSectorExitType { get; set; }

        [XmlElement(ElementName = "m_iSectorExitID")]
        public M_iSectorExitID M_iSectorExitID { get; set; }

        [XmlElement(ElementName = "m_iRelativeSectorID")]
        public M_iRelativeSectorID M_iRelativeSectorID { get; set; }

        [XmlElement(ElementName = "m_iConditionKillRatio_1Step")]
        public M_iConditionKillRatio_1Step M_iConditionKillRatio_1Step { get; set; }

        [XmlElement(ElementName = "m_iConditionKillRatio_2Step")]
        public M_iConditionKillRatio_2Step M_iConditionKillRatio_2Step { get; set; }

        [XmlElement(ElementName = "m_iConditionKillRatio_3Step")]
        public M_iConditionKillRatio_3Step M_iConditionKillRatio_3Step { get; set; }

        [XmlElement(ElementName = "m_iConditionKillRatio_4Step")]
        public M_iConditionKillRatio_4Step M_iConditionKillRatio_4Step { get; set; }

        [XmlElement(ElementName = "m_iConditionKillRatio_5Step")]
        public M_iConditionKillRatio_5Step M_iConditionKillRatio_5Step { get; set; }

        [XmlAttribute(AttributeName = "boxtype")]
        public string Boxtype { get; set; }
    }

    [XmlRoot(ElementName = "m_szFunction")]
    public class M_szFunction
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }

        [XmlAttribute(AttributeName = "valuetype")]
        public string Valuetype { get; set; }
    }

    [XmlRoot(ElementName = "m_iCheckID")]
    public class M_iCheckID
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "VLuaFunctionBox")]
    public class VLuaFunctionBox
    {
        [XmlElement(ElementName = "m_ID")]
        public M_ID M_ID { get; set; }

        [XmlElement(ElementName = "m_iGenerateID")]
        public M_iGenerateID M_iGenerateID { get; set; }

        [XmlElement(ElementName = "iLayerBitmask")]
        public ILayerBitmask ILayerBitmask { get; set; }

        [XmlElement(ElementName = "m_vPosTopLeft")]
        public M_vPosTopLeft M_vPosTopLeft { get; set; }

        [XmlElement(ElementName = "m_vPosBottomRight")]
        public M_vPosBottomRight M_vPosBottomRight { get; set; }

        [XmlElement(ElementName = "m_fRotation")]
        public M_fRotation M_fRotation { get; set; }

        [XmlElement(ElementName = "m_vSize")]
        public M_vSize M_vSize { get; set; }

        [XmlElement(ElementName = "m_bShowCustomEntity")]
        public M_bShowCustomEntity M_bShowCustomEntity { get; set; }

        [XmlElement(ElementName = "m_ColorLDR")]
        public M_ColorLDR M_ColorLDR { get; set; }

        [XmlElement(ElementName = "m_eType")]
        public M_eType M_eType { get; set; }

        [XmlElement(ElementName = "m_szFunction")]
        public M_szFunction M_szFunction { get; set; }

        [XmlElement(ElementName = "m_iCheckID")]
        public M_iCheckID M_iCheckID { get; set; }

        [XmlAttribute(AttributeName = "boxtype")]
        public string Boxtype { get; set; }
    }

    [XmlRoot(ElementName = "m_iLoopCount")]
    public class M_iLoopCount
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iEntityID")]
    public class M_iEntityID
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iCheckBox_0")]
    public class M_iCheckBox_0
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iCheckBox_1")]
    public class M_iCheckBox_1
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iCheckBox_2")]
    public class M_iCheckBox_2
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iCheckBox_3")]
    public class M_iCheckBox_3
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iCheckBox_4")]
    public class M_iCheckBox_4
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iCheckBox_5")]
    public class M_iCheckBox_5
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iCheckBox_6")]
    public class M_iCheckBox_6
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iCheckBox_7")]
    public class M_iCheckBox_7
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iCheckBox_8")]
    public class M_iCheckBox_8
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iCheckBox_9")]
    public class M_iCheckBox_9
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "VCheckMonsterSpawnBox")]
    public class VCheckMonsterSpawnBox
    {
        [XmlElement(ElementName = "m_ID")]
        public M_ID M_ID { get; set; }

        [XmlElement(ElementName = "m_iGenerateID")]
        public M_iGenerateID M_iGenerateID { get; set; }

        [XmlElement(ElementName = "iLayerBitmask")]
        public ILayerBitmask ILayerBitmask { get; set; }

        [XmlElement(ElementName = "m_vPosTopLeft")]
        public M_vPosTopLeft M_vPosTopLeft { get; set; }

        [XmlElement(ElementName = "m_vPosBottomRight")]
        public M_vPosBottomRight M_vPosBottomRight { get; set; }

        [XmlElement(ElementName = "m_fRotation")]
        public M_fRotation M_fRotation { get; set; }

        [XmlElement(ElementName = "m_vSize")]
        public M_vSize M_vSize { get; set; }

        [XmlElement(ElementName = "m_bShowCustomEntity")]
        public M_bShowCustomEntity M_bShowCustomEntity { get; set; }

        [XmlElement(ElementName = "m_ColorLDR")]
        public M_ColorLDR M_ColorLDR { get; set; }

        [XmlElement(ElementName = "m_eType")]
        public M_eType M_eType { get; set; }

        [XmlElement(ElementName = "m_iLoopCount")]
        public M_iLoopCount M_iLoopCount { get; set; }

        [XmlElement(ElementName = "m_iEntityID")]
        public M_iEntityID M_iEntityID { get; set; }

        [XmlElement(ElementName = "m_iCheckBox_0")]
        public M_iCheckBox_0 M_iCheckBox_0 { get; set; }

        [XmlElement(ElementName = "m_iCheckBox_1")]
        public M_iCheckBox_1 M_iCheckBox_1 { get; set; }

        [XmlElement(ElementName = "m_iCheckBox_2")]
        public M_iCheckBox_2 M_iCheckBox_2 { get; set; }

        [XmlElement(ElementName = "m_iCheckBox_3")]
        public M_iCheckBox_3 M_iCheckBox_3 { get; set; }

        [XmlElement(ElementName = "m_iCheckBox_4")]
        public M_iCheckBox_4 M_iCheckBox_4 { get; set; }

        [XmlElement(ElementName = "m_iCheckBox_5")]
        public M_iCheckBox_5 M_iCheckBox_5 { get; set; }

        [XmlElement(ElementName = "m_iCheckBox_6")]
        public M_iCheckBox_6 M_iCheckBox_6 { get; set; }

        [XmlElement(ElementName = "m_iCheckBox_7")]
        public M_iCheckBox_7 M_iCheckBox_7 { get; set; }

        [XmlElement(ElementName = "m_iCheckBox_8")]
        public M_iCheckBox_8 M_iCheckBox_8 { get; set; }

        [XmlElement(ElementName = "m_iCheckBox_9")]
        public M_iCheckBox_9 M_iCheckBox_9 { get; set; }

        [XmlAttribute(AttributeName = "boxtype")]
        public string Boxtype { get; set; }
    }

    [XmlRoot(ElementName = "m_eType1")]
    public class M_eType1
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iMonsterID1")]
    public class M_iMonsterID1
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iChance1")]
    public class M_iChance1
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_eType2")]
    public class M_eType2
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iMonsterID2")]
    public class M_iMonsterID2
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iChance2")]
    public class M_iChance2
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_eType3")]
    public class M_eType3
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iMonsterID3")]
    public class M_iMonsterID3
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iChance3")]
    public class M_iChance3
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_eType4")]
    public class M_eType4
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iMonsterID4")]
    public class M_iMonsterID4
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iChance4")]
    public class M_iChance4
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_eType5")]
    public class M_eType5
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iMonsterID5")]
    public class M_iMonsterID5
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iChance5")]
    public class M_iChance5
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_eType6")]
    public class M_eType6
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iMonsterID6")]
    public class M_iMonsterID6
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iChance6")]
    public class M_iChance6
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_eType7")]
    public class M_eType7
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iMonsterID7")]
    public class M_iMonsterID7
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iChance7")]
    public class M_iChance7
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_eType8")]
    public class M_eType8
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iMonsterID8")]
    public class M_iMonsterID8
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iChance8")]
    public class M_iChance8
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_eType9")]
    public class M_eType9
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iMonsterID9")]
    public class M_iMonsterID9
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iChance9")]
    public class M_iChance9
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_eType10")]
    public class M_eType10
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iMonsterID10")]
    public class M_iMonsterID10
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iChance10")]
    public class M_iChance10
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_eCreationPositionType")]
    public class M_eCreationPositionType
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_eMoveType")]
    public class M_eMoveType
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_eCreationCondition")]
    public class M_eCreationCondition
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_CreationEffectFile")]
    public class M_CreationEffectFile
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_fWaitCreationDelayTime")]
    public class M_fWaitCreationDelayTime
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_fWaitCreationSequenceTime")]
    public class M_fWaitCreationSequenceTime
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iWaitCreationMaxWave")]
    public class M_iWaitCreationMaxWave
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iMaxEntityCount")]
    public class M_iMaxEntityCount
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_eWaitCreationSequenceType")]
    public class M_eWaitCreationSequenceType
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iWaypoint")]
    public class M_iWaypoint
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iAggroGroupID")]
    public class M_iAggroGroupID
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iAggroDistance")]
    public class M_iAggroDistance
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iAggroMaxCount")]
    public class M_iAggroMaxCount
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_fLookRatio")]
    public class M_fLookRatio
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_bShowSight")]
    public class M_bShowSight
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_szObjectKey")]
    public class M_szObjectKey
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_eScriptType")]
    public class M_eScriptType
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iCheckScriptHP1")]
    public class M_iCheckScriptHP1
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iCheckScriptHP2")]
    public class M_iCheckScriptHP2
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iCheckScriptHP3")]
    public class M_iCheckScriptHP3
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iCheckScriptHP4")]
    public class M_iCheckScriptHP4
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iCheckScriptHP5")]
    public class M_iCheckScriptHP5
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iSectorID")]
    public class M_iSectorID
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_ChangeSpawnAction")]
    public class M_ChangeSpawnAction
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_ProtectionTarget")]
    public class M_ProtectionTarget
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_RespawnTime")]
    public class M_RespawnTime
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iStep")]
    public class M_iStep
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_eRespawnType")]
    public class M_eRespawnType
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iRespawnCondition")]
    public class M_iRespawnCondition
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iGroupID")]
    public class M_iGroupID
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "VMonsterSpawnBox")]
    public class VMonsterSpawnBox
    {
        [XmlElement(ElementName = "m_ID")]
        public M_ID M_ID { get; set; }

        [XmlElement(ElementName = "m_iGenerateID")]
        public M_iGenerateID M_iGenerateID { get; set; }

        [XmlElement(ElementName = "iLayerBitmask")]
        public ILayerBitmask ILayerBitmask { get; set; }

        [XmlElement(ElementName = "m_vPosTopLeft")]
        public M_vPosTopLeft M_vPosTopLeft { get; set; }

        [XmlElement(ElementName = "m_vPosBottomRight")]
        public M_vPosBottomRight M_vPosBottomRight { get; set; }

        [XmlElement(ElementName = "m_fRotation")]
        public M_fRotation M_fRotation { get; set; }

        [XmlElement(ElementName = "m_vSize")]
        public M_vSize M_vSize { get; set; }

        [XmlElement(ElementName = "m_bShowCustomEntity")]
        public M_bShowCustomEntity M_bShowCustomEntity { get; set; }

        [XmlElement(ElementName = "m_ColorLDR")]
        public M_ColorLDR M_ColorLDR { get; set; }

        [XmlElement(ElementName = "m_eType1")]
        public M_eType1 M_eType1 { get; set; }

        [XmlElement(ElementName = "m_iMonsterID1")]
        public M_iMonsterID1 M_iMonsterID1 { get; set; }

        [XmlElement(ElementName = "m_iChance1")]
        public M_iChance1 M_iChance1 { get; set; }

        [XmlElement(ElementName = "m_eType2")]
        public M_eType2 M_eType2 { get; set; }

        [XmlElement(ElementName = "m_iMonsterID2")]
        public M_iMonsterID2 M_iMonsterID2 { get; set; }

        [XmlElement(ElementName = "m_iChance2")]
        public M_iChance2 M_iChance2 { get; set; }

        [XmlElement(ElementName = "m_eType3")]
        public M_eType3 M_eType3 { get; set; }

        [XmlElement(ElementName = "m_iMonsterID3")]
        public M_iMonsterID3 M_iMonsterID3 { get; set; }

        [XmlElement(ElementName = "m_iChance3")]
        public M_iChance3 M_iChance3 { get; set; }

        [XmlElement(ElementName = "m_eType4")]
        public M_eType4 M_eType4 { get; set; }

        [XmlElement(ElementName = "m_iMonsterID4")]
        public M_iMonsterID4 M_iMonsterID4 { get; set; }

        [XmlElement(ElementName = "m_iChance4")]
        public M_iChance4 M_iChance4 { get; set; }

        [XmlElement(ElementName = "m_eType5")]
        public M_eType5 M_eType5 { get; set; }

        [XmlElement(ElementName = "m_iMonsterID5")]
        public M_iMonsterID5 M_iMonsterID5 { get; set; }

        [XmlElement(ElementName = "m_iChance5")]
        public M_iChance5 M_iChance5 { get; set; }

        [XmlElement(ElementName = "m_eType6")]
        public M_eType6 M_eType6 { get; set; }

        [XmlElement(ElementName = "m_iMonsterID6")]
        public M_iMonsterID6 M_iMonsterID6 { get; set; }

        [XmlElement(ElementName = "m_iChance6")]
        public M_iChance6 M_iChance6 { get; set; }

        [XmlElement(ElementName = "m_eType7")]
        public M_eType7 M_eType7 { get; set; }

        [XmlElement(ElementName = "m_iMonsterID7")]
        public M_iMonsterID7 M_iMonsterID7 { get; set; }

        [XmlElement(ElementName = "m_iChance7")]
        public M_iChance7 M_iChance7 { get; set; }

        [XmlElement(ElementName = "m_eType8")]
        public M_eType8 M_eType8 { get; set; }

        [XmlElement(ElementName = "m_iMonsterID8")]
        public M_iMonsterID8 M_iMonsterID8 { get; set; }

        [XmlElement(ElementName = "m_iChance8")]
        public M_iChance8 M_iChance8 { get; set; }

        [XmlElement(ElementName = "m_eType9")]
        public M_eType9 M_eType9 { get; set; }

        [XmlElement(ElementName = "m_iMonsterID9")]
        public M_iMonsterID9 M_iMonsterID9 { get; set; }

        [XmlElement(ElementName = "m_iChance9")]
        public M_iChance9 M_iChance9 { get; set; }

        [XmlElement(ElementName = "m_eType10")]
        public M_eType10 M_eType10 { get; set; }

        [XmlElement(ElementName = "m_iMonsterID10")]
        public M_iMonsterID10 M_iMonsterID10 { get; set; }

        [XmlElement(ElementName = "m_iChance10")]
        public M_iChance10 M_iChance10 { get; set; }

        [XmlElement(ElementName = "m_eCreationPositionType")]
        public M_eCreationPositionType M_eCreationPositionType { get; set; }

        [XmlElement(ElementName = "m_eMoveType")]
        public M_eMoveType M_eMoveType { get; set; }

        [XmlElement(ElementName = "m_eCreationCondition")]
        public M_eCreationCondition M_eCreationCondition { get; set; }

        [XmlElement(ElementName = "m_CreationEffectFile")]
        public M_CreationEffectFile M_CreationEffectFile { get; set; }

        [XmlElement(ElementName = "m_fWaitCreationDelayTime")]
        public M_fWaitCreationDelayTime M_fWaitCreationDelayTime { get; set; }

        [XmlElement(ElementName = "m_fWaitCreationSequenceTime")]
        public M_fWaitCreationSequenceTime M_fWaitCreationSequenceTime { get; set; }

        [XmlElement(ElementName = "m_iWaitCreationMaxWave")]
        public M_iWaitCreationMaxWave M_iWaitCreationMaxWave { get; set; }

        [XmlElement(ElementName = "m_iMaxEntityCount")]
        public M_iMaxEntityCount M_iMaxEntityCount { get; set; }

        [XmlElement(ElementName = "m_eWaitCreationSequenceType")]
        public M_eWaitCreationSequenceType M_eWaitCreationSequenceType { get; set; }

        [XmlElement(ElementName = "m_iWaypoint")]
        public M_iWaypoint M_iWaypoint { get; set; }

        [XmlElement(ElementName = "m_iAggroGroupID")]
        public M_iAggroGroupID M_iAggroGroupID { get; set; }

        [XmlElement(ElementName = "m_iAggroDistance")]
        public M_iAggroDistance M_iAggroDistance { get; set; }

        [XmlElement(ElementName = "m_iAggroMaxCount")]
        public M_iAggroMaxCount M_iAggroMaxCount { get; set; }

        [XmlElement(ElementName = "m_fLookRatio")]
        public M_fLookRatio M_fLookRatio { get; set; }

        [XmlElement(ElementName = "m_bShowSight")]
        public M_bShowSight M_bShowSight { get; set; }

        [XmlElement(ElementName = "m_szObjectKey")]
        public M_szObjectKey M_szObjectKey { get; set; }

        [XmlElement(ElementName = "m_eScriptType")]
        public M_eScriptType M_eScriptType { get; set; }

        [XmlElement(ElementName = "m_iCheckScriptHP1")]
        public M_iCheckScriptHP1 M_iCheckScriptHP1 { get; set; }

        [XmlElement(ElementName = "m_iCheckScriptHP2")]
        public M_iCheckScriptHP2 M_iCheckScriptHP2 { get; set; }

        [XmlElement(ElementName = "m_iCheckScriptHP3")]
        public M_iCheckScriptHP3 M_iCheckScriptHP3 { get; set; }

        [XmlElement(ElementName = "m_iCheckScriptHP4")]
        public M_iCheckScriptHP4 M_iCheckScriptHP4 { get; set; }

        [XmlElement(ElementName = "m_iCheckScriptHP5")]
        public M_iCheckScriptHP5 M_iCheckScriptHP5 { get; set; }

        [XmlElement(ElementName = "m_iSectorID")]
        public M_iSectorID M_iSectorID { get; set; }

        [XmlElement(ElementName = "m_ChangeSpawnAction")]
        public M_ChangeSpawnAction M_ChangeSpawnAction { get; set; }

        [XmlElement(ElementName = "m_ProtectionTarget")]
        public M_ProtectionTarget M_ProtectionTarget { get; set; }

        [XmlElement(ElementName = "m_RespawnTime")]
        public M_RespawnTime M_RespawnTime { get; set; }

        [XmlElement(ElementName = "m_iStep")]
        public M_iStep M_iStep { get; set; }

        [XmlElement(ElementName = "m_eRespawnType")]
        public M_eRespawnType M_eRespawnType { get; set; }

        [XmlElement(ElementName = "m_iRespawnCondition")]
        public M_iRespawnCondition M_iRespawnCondition { get; set; }

        [XmlElement(ElementName = "m_iGroupID")]
        public M_iGroupID M_iGroupID { get; set; }

        [XmlAttribute(AttributeName = "boxtype")]
        public string Boxtype { get; set; }
    }

    [XmlRoot(ElementName = "m_eEvent_Type")]
    public class M_eEvent_Type
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_fEvent_Rate")]
    public class M_fEvent_Rate
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_fEvent_Delay_Time")]
    public class M_fEvent_Delay_Time
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iEvent_Operation_ID")]
    public class M_iEvent_Operation_ID
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_fEvent_Time")]
    public class M_fEvent_Time
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iSpawn_Box_ID_0")]
    public class M_iSpawn_Box_ID_0
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iSpawn_Box_ID_1")]
    public class M_iSpawn_Box_ID_1
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iSpawn_Box_ID_2")]
    public class M_iSpawn_Box_ID_2
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iSpawn_Box_ID_3")]
    public class M_iSpawn_Box_ID_3
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iSpawn_Box_ID_4")]
    public class M_iSpawn_Box_ID_4
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "VCheckEventSpawnBox")]
    public class VCheckEventSpawnBox
    {
        [XmlElement(ElementName = "m_ID")]
        public M_ID M_ID { get; set; }

        [XmlElement(ElementName = "m_iGenerateID")]
        public M_iGenerateID M_iGenerateID { get; set; }

        [XmlElement(ElementName = "iLayerBitmask")]
        public ILayerBitmask ILayerBitmask { get; set; }

        [XmlElement(ElementName = "m_vPosTopLeft")]
        public M_vPosTopLeft M_vPosTopLeft { get; set; }

        [XmlElement(ElementName = "m_vPosBottomRight")]
        public M_vPosBottomRight M_vPosBottomRight { get; set; }

        [XmlElement(ElementName = "m_fRotation")]
        public M_fRotation M_fRotation { get; set; }

        [XmlElement(ElementName = "m_vSize")]
        public M_vSize M_vSize { get; set; }

        [XmlElement(ElementName = "m_bShowCustomEntity")]
        public M_bShowCustomEntity M_bShowCustomEntity { get; set; }

        [XmlElement(ElementName = "m_ColorLDR")]
        public M_ColorLDR M_ColorLDR { get; set; }

        [XmlElement(ElementName = "m_eEvent_Type")]
        public M_eEvent_Type M_eEvent_Type { get; set; }

        [XmlElement(ElementName = "m_fEvent_Rate")]
        public M_fEvent_Rate M_fEvent_Rate { get; set; }

        [XmlElement(ElementName = "m_fEvent_Delay_Time")]
        public M_fEvent_Delay_Time M_fEvent_Delay_Time { get; set; }

        [XmlElement(ElementName = "m_iEvent_Operation_ID")]
        public M_iEvent_Operation_ID M_iEvent_Operation_ID { get; set; }

        [XmlElement(ElementName = "m_fEvent_Time")]
        public M_fEvent_Time M_fEvent_Time { get; set; }

        [XmlElement(ElementName = "m_iSpawn_Box_ID_0")]
        public M_iSpawn_Box_ID_0 M_iSpawn_Box_ID_0 { get; set; }

        [XmlElement(ElementName = "m_iSpawn_Box_ID_1")]
        public M_iSpawn_Box_ID_1 M_iSpawn_Box_ID_1 { get; set; }

        [XmlElement(ElementName = "m_iSpawn_Box_ID_2")]
        public M_iSpawn_Box_ID_2 M_iSpawn_Box_ID_2 { get; set; }

        [XmlElement(ElementName = "m_iSpawn_Box_ID_3")]
        public M_iSpawn_Box_ID_3 M_iSpawn_Box_ID_3 { get; set; }

        [XmlElement(ElementName = "m_iSpawn_Box_ID_4")]
        public M_iSpawn_Box_ID_4 M_iSpawn_Box_ID_4 { get; set; }

        [XmlAttribute(AttributeName = "boxtype")]
        public string Boxtype { get; set; }
    }

    [XmlRoot(ElementName = "m_eEntityType")]
    public class M_eEntityType
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iGroup")]
    public class M_iGroup
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "VCommonPositionBox")]
    public class VCommonPositionBox
    {
        [XmlElement(ElementName = "m_ID")]
        public M_ID M_ID { get; set; }

        [XmlElement(ElementName = "m_iGenerateID")]
        public M_iGenerateID M_iGenerateID { get; set; }

        [XmlElement(ElementName = "iLayerBitmask")]
        public ILayerBitmask ILayerBitmask { get; set; }

        [XmlElement(ElementName = "m_vPosTopLeft")]
        public M_vPosTopLeft M_vPosTopLeft { get; set; }

        [XmlElement(ElementName = "m_vPosBottomRight")]
        public M_vPosBottomRight M_vPosBottomRight { get; set; }

        [XmlElement(ElementName = "m_fRotation")]
        public M_fRotation M_fRotation { get; set; }

        [XmlElement(ElementName = "m_vSize")]
        public M_vSize M_vSize { get; set; }

        [XmlElement(ElementName = "m_bShowCustomEntity")]
        public M_bShowCustomEntity M_bShowCustomEntity { get; set; }

        [XmlElement(ElementName = "m_ColorLDR")]
        public M_ColorLDR M_ColorLDR { get; set; }

        [XmlElement(ElementName = "m_eEntityType")]
        public M_eEntityType M_eEntityType { get; set; }

        [XmlElement(ElementName = "m_iEntityID")]
        public M_iEntityID M_iEntityID { get; set; }

        [XmlElement(ElementName = "m_iGroup")]
        public M_iGroup M_iGroup { get; set; }

        [XmlAttribute(AttributeName = "boxtype")]
        public string Boxtype { get; set; }
    }

    [XmlRoot(ElementName = "m_iInteractionID")]
    public class M_iInteractionID
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_sObjectKey")]
    public class M_sObjectKey
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "VInterActionBox")]
    public class VInterActionBox
    {
        [XmlElement(ElementName = "m_ID")]
        public M_ID M_ID { get; set; }

        [XmlElement(ElementName = "m_iGenerateID")]
        public M_iGenerateID M_iGenerateID { get; set; }

        [XmlElement(ElementName = "iLayerBitmask")]
        public ILayerBitmask ILayerBitmask { get; set; }

        [XmlElement(ElementName = "m_vPosTopLeft")]
        public M_vPosTopLeft M_vPosTopLeft { get; set; }

        [XmlElement(ElementName = "m_vPosBottomRight")]
        public M_vPosBottomRight M_vPosBottomRight { get; set; }

        [XmlElement(ElementName = "m_fRotation")]
        public M_fRotation M_fRotation { get; set; }

        [XmlElement(ElementName = "m_vSize")]
        public M_vSize M_vSize { get; set; }

        [XmlElement(ElementName = "m_bShowCustomEntity")]
        public M_bShowCustomEntity M_bShowCustomEntity { get; set; }

        [XmlElement(ElementName = "m_ColorLDR")]
        public M_ColorLDR M_ColorLDR { get; set; }

        [XmlElement(ElementName = "m_iInteractionID")]
        public M_iInteractionID M_iInteractionID { get; set; }

        [XmlElement(ElementName = "m_sObjectKey")]
        public M_sObjectKey M_sObjectKey { get; set; }

        [XmlAttribute(AttributeName = "boxtype")]
        public string Boxtype { get; set; }
    }

    [XmlRoot(ElementName = "m_nSectorID")]
    public class M_nSectorID
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "VSectorStartBox")]
    public class VSectorStartBox
    {
        [XmlElement(ElementName = "m_ID")]
        public M_ID M_ID { get; set; }

        [XmlElement(ElementName = "m_iGenerateID")]
        public M_iGenerateID M_iGenerateID { get; set; }

        [XmlElement(ElementName = "iLayerBitmask")]
        public ILayerBitmask ILayerBitmask { get; set; }

        [XmlElement(ElementName = "m_vPosTopLeft")]
        public M_vPosTopLeft M_vPosTopLeft { get; set; }

        [XmlElement(ElementName = "m_vPosBottomRight")]
        public M_vPosBottomRight M_vPosBottomRight { get; set; }

        [XmlElement(ElementName = "m_fRotation")]
        public M_fRotation M_fRotation { get; set; }

        [XmlElement(ElementName = "m_vSize")]
        public M_vSize M_vSize { get; set; }

        [XmlElement(ElementName = "m_bShowCustomEntity")]
        public M_bShowCustomEntity M_bShowCustomEntity { get; set; }

        [XmlElement(ElementName = "m_ColorLDR")]
        public M_ColorLDR M_ColorLDR { get; set; }

        [XmlElement(ElementName = "m_nSectorID")]
        public M_nSectorID M_nSectorID { get; set; }

        [XmlAttribute(AttributeName = "boxtype")]
        public string Boxtype { get; set; }
    }

    [XmlRoot(ElementName = "Batchs")]
    public class Batchs
    {
        [XmlElement(ElementName = "VStartEventBox")]
        public VStartEventBox VStartEventBox { get; set; }

        [XmlElement(ElementName = "VPortalExitBox")]
        public List<VPortalExitBox> VPortalExitBox { get; set; }

        [XmlElement(ElementName = "VMazeEscapeBox")]
        public VMazeEscapeBox VMazeEscapeBox { get; set; }

        [XmlElement(ElementName = "VPortalBox")]
        public List<VPortalBox> VPortalBox { get; set; }

        [XmlElement(ElementName = "VSectorBox")]
        public List<VSectorBox> VSectorBox { get; set; }

        [XmlElement(ElementName = "VLuaFunctionBox")]
        public List<VLuaFunctionBox> VLuaFunctionBox { get; set; }

        [XmlElement(ElementName = "VCheckMonsterSpawnBox")]
        public List<VCheckMonsterSpawnBox> VCheckMonsterSpawnBox { get; set; }

        [XmlElement(ElementName = "VMonsterSpawnBox")]
        public List<VMonsterSpawnBox> VMonsterSpawnBox { get; set; }

        [XmlElement(ElementName = "VCheckEventSpawnBox")]
        public List<VCheckEventSpawnBox> VCheckEventSpawnBox { get; set; }

        [XmlElement(ElementName = "VCommonPositionBox")]
        public VCommonPositionBox VCommonPositionBox { get; set; }

        [XmlElement(ElementName = "VInterActionBox")]
        public List<VInterActionBox> VInterActionBox { get; set; }

        [XmlElement(ElementName = "VSectorStartBox")]
        public List<VSectorStartBox> VSectorStartBox { get; set; }

        [XmlAttribute(AttributeName = "eventtype")]
        public string Eventtype { get; set; }

        [XmlAttribute(AttributeName = "eventcount")]
        public string Eventcount { get; set; }

        [XmlElement(ElementName = "VEscortPoint")]
        public List<VEscortPoint> VEscortPoint { get; set; }
    }

    [XmlRoot(ElementName = "m_eBattleType")]
    public class M_eBattleType
    {
        [XmlAttribute(AttributeName = "valuetype")]
        public string Valuetype { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iBeforePoint")]
    public class M_iBeforePoint
    {
        [XmlAttribute(AttributeName = "valuetype")]
        public string Valuetype { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iNextPoint")]
    public class M_iNextPoint
    {
        [XmlAttribute(AttributeName = "valuetype")]
        public string Valuetype { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iNextPoint2")]
    public class M_iNextPoint2
    {
        [XmlAttribute(AttributeName = "valuetype")]
        public string Valuetype { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iNextPoint3")]
    public class M_iNextPoint3
    {
        [XmlAttribute(AttributeName = "valuetype")]
        public string Valuetype { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_iNextPoint4")]
    public class M_iNextPoint4
    {
        [XmlAttribute(AttributeName = "valuetype")]
        public string Valuetype { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_szIdleAction")]
    public class M_szIdleAction
    {
        [XmlAttribute(AttributeName = "valuetype")]
        public string Valuetype { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_uiIdleActionRatio")]
    public class M_uiIdleActionRatio
    {
        [XmlAttribute(AttributeName = "valuetype")]
        public string Valuetype { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_uiDelayTime")]
    public class M_uiDelayTime
    {
        [XmlAttribute(AttributeName = "valuetype")]
        public string Valuetype { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_RepeatCount")]
    public class M_RepeatCount
    {
        [XmlAttribute(AttributeName = "valuetype")]
        public string Valuetype { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_sEnableEffectPath")]
    public class M_sEnableEffectPath
    {
        [XmlAttribute(AttributeName = "valuetype")]
        public string Valuetype { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "m_sDisableEffectPath")]
    public class M_sDisableEffectPath
    {
        [XmlAttribute(AttributeName = "valuetype")]
        public string Valuetype { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "VEscortPoint")]
    public class VEscortPoint
    {
        [XmlElement(ElementName = "m_ID")]
        public M_ID M_ID { get; set; }

        [XmlElement(ElementName = "m_iGenerateID")]
        public M_iGenerateID M_iGenerateID { get; set; }

        [XmlElement(ElementName = "iLayerBitmask")]
        public ILayerBitmask ILayerBitmask { get; set; }

        [XmlElement(ElementName = "m_vPosTopLeft")]
        public M_vPosTopLeft M_vPosTopLeft { get; set; }

        [XmlElement(ElementName = "m_vPosBottomRight")]
        public M_vPosBottomRight M_vPosBottomRight { get; set; }

        [XmlElement(ElementName = "m_fRotation")]
        public M_fRotation M_fRotation { get; set; }

        [XmlElement(ElementName = "m_vSize")]
        public M_vSize M_vSize { get; set; }

        [XmlElement(ElementName = "m_bShowCustomEntity")]
        public M_bShowCustomEntity M_bShowCustomEntity { get; set; }

        [XmlElement(ElementName = "m_ColorLDR")]
        public M_ColorLDR M_ColorLDR { get; set; }

        [XmlElement(ElementName = "m_eType")]
        public M_eType M_eType { get; set; }

        [XmlElement(ElementName = "m_eBattleType")]
        public M_eBattleType M_eBattleType { get; set; }

        [XmlElement(ElementName = "m_iBeforePoint")]
        public M_iBeforePoint M_iBeforePoint { get; set; }

        [XmlElement(ElementName = "m_iNextPoint")]
        public M_iNextPoint M_iNextPoint { get; set; }

        [XmlElement(ElementName = "m_iNextPoint2")]
        public M_iNextPoint2 M_iNextPoint2 { get; set; }

        [XmlElement(ElementName = "m_iNextPoint3")]
        public M_iNextPoint3 M_iNextPoint3 { get; set; }

        [XmlElement(ElementName = "m_iNextPoint4")]
        public M_iNextPoint4 M_iNextPoint4 { get; set; }

        [XmlElement(ElementName = "m_szIdleAction")]
        public M_szIdleAction M_szIdleAction { get; set; }

        [XmlElement(ElementName = "m_uiIdleActionRatio")]
        public M_uiIdleActionRatio M_uiIdleActionRatio { get; set; }

        [XmlElement(ElementName = "m_uiDelayTime")]
        public M_uiDelayTime M_uiDelayTime { get; set; }

        [XmlElement(ElementName = "m_RepeatCount")]
        public M_RepeatCount M_RepeatCount { get; set; }

        [XmlElement(ElementName = "m_szFunction")]
        public M_szFunction M_szFunction { get; set; }

        [XmlElement(ElementName = "m_sEnableEffectPath")]
        public M_sEnableEffectPath M_sEnableEffectPath { get; set; }

        [XmlElement(ElementName = "m_sDisableEffectPath")]
        public M_sDisableEffectPath M_sDisableEffectPath { get; set; }

        [XmlAttribute(AttributeName = "pointtype")]
        public string Pointtype { get; set; }
    }

    [XmlRoot(ElementName = "root")]
    public class Root
    {
        [XmlElement(ElementName = "Batchs")]
        public List<Batchs> Batchs { get; set; }
    }
}
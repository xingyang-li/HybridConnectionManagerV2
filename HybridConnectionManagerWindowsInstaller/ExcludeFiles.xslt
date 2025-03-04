<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:wix="http://wixtoolset.org/schemas/v4/wxs">

	<!-- Store IDs of components to be removed -->
	<xsl:key name="components-to-remove"
			 match="wix:Component[
                  contains(wix:File/@Source, '.pdb') or
                  contains(wix:File/@Source, '.ilk') or
                  contains(wix:File/@Source, '.ipdb') or
                  contains(wix:File/@Source, '.iobj') or
                  contains(wix:File/@Source, '.log') or
                  contains(wix:File/@Source, '.map') or
                  contains(wix:File/@Source, '.vshost.exe') or
                  contains(wix:File/@Source, '.xml') or
                  contains(wix:File/@Source, 'debug\') or
                  contains(wix:File/@Source, '.sample.') or
                  contains(wix:File/@Source, '.tmp') or 
			      contains(wix:File/@Source, '.staticwebassets.endpoints.json')
                  ]"
			 use="@Id"/>

	<!-- Identity transform (copy everything by default) -->
	<xsl:template match="@*|node()">
		<xsl:copy>
			<xsl:apply-templates select="@*|node()"/>
		</xsl:copy>
	</xsl:template>

	<!-- Remove unwanted components -->
	<xsl:template match="wix:Component[key('components-to-remove', @Id)]"/>

	<!-- Remove references to the unwanted components -->
	<xsl:template match="wix:ComponentRef[key('components-to-remove', @Id)]"/>

	<!-- Process ComponentGroup to remove unwanted ComponentRefs -->
	<xsl:template match="wix:ComponentGroup">
		<xsl:copy>
			<xsl:apply-templates select="@*"/>
			<xsl:for-each select="*">
				<xsl:choose>
					<!-- Skip ComponentRefs that point to components we're removing -->
					<xsl:when test="self::wix:ComponentRef and key('components-to-remove', @Id)">
						<!-- Optionally add a comment to mark what was removed -->
						<xsl:comment>
							Removed reference to <xsl:value-of select="@Id"/>
						</xsl:comment>
					</xsl:when>
					<xsl:otherwise>
						<xsl:apply-templates select="."/>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:for-each>
		</xsl:copy>
	</xsl:template>

	<!-- Maintain empty directories if desired (comment this out if you want to remove empty dirs) -->
	<xsl:template match="wix:Directory">
		<xsl:copy>
			<xsl:apply-templates select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>

</xsl:stylesheet>
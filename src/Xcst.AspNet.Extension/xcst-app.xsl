<?xml version="1.0" encoding="utf-8"?>
<!--
 Copyright 2015 Max Toro Q.

 Licensed under the Apache License, Version 2.0 (the "License");
 you may not use this file except in compliance with the License.
 You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing permissions and
 limitations under the License.
-->
<stylesheet version="2.0" exclude-result-prefixes="#all"
   xmlns="http://www.w3.org/1999/XSL/Transform"
   xmlns:xs="http://www.w3.org/2001/XMLSchema"
   xmlns:c="http://maxtoroq.github.io/XCST"
   xmlns:a="http://maxtoroq.github.io/XCST/application"
   xmlns:xcst="http://maxtoroq.github.io/XCST/grammar"
   xmlns:code="http://maxtoroq.github.io/XCST/code"
   xmlns:src="http://maxtoroq.github.io/XCST/compiled">

   <import href="../../../XCST/src/Xcst.Compiler/CodeGeneration/xcst-metadata.xsl" use-when="false()"/>
   <import href="../../../XCST/src/Xcst.Compiler/CodeGeneration/xcst-core.xsl" use-when="false()"/>

   <param name="a:application-uri" as="xs:anyURI?"/>
   <param name="a:aspnetlib" select="true()" as="xs:boolean"/>
   <param name="a:page-type" as="element(code:type-reference)?"/>
   <param name="a:default-model" as="element(code:type-reference)?"/>
   <param name="a:default-model-dynamic" select="false()" as="xs:boolean"/>

   <variable name="a:html-attributes" select="'class', 'attributes'"/>
   <variable name="a:input-attributes" select="'for', 'name', 'value', 'disabled', 'autofocus', $a:html-attributes"/>
   <variable name="a:text-box-attributes" select="'readonly', 'placeholder', $a:input-attributes"/>
   <variable name="a:href-fn" select="$a:aspnetlib and not($src:named-package) and $a:application-uri"/>

   <!--
      ## Forms
   -->

   <template match="a:input | a:hidden" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <variable name="hidden" select="self::a:hidden"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="
            if ($hidden) then $a:input-attributes[. ne 'autofocus']
            else $a:text-box-attributes, 'format', 'type'"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for">
         <with-param name="attribs" select="@name, @value"/>
      </call-template>

      <variable name="output-is-doc" select="src:output-is-doc($output)"/>
      <variable name="doc-output" select="src:doc-output(., $output)"/>

      <if test="not($output-is-doc)">
         <code:variable name="{$doc-output/src:reference/code:*/@name}">
            <code:method-call name="CastElement">
               <sequence select="src:helper-type('DocumentWriter')"/>
               <code:arguments>
                  <sequence select="$output/src:reference/code:*"/>
               </code:arguments>
            </code:method-call>
         </code:variable>
      </if>

      <variable name="for-model" select="empty((@for, @name))"/>

      <code:method-call name="Input{'For'[current()/@for], 'ForModel'[$for-model]}">
         <sequence select="a:helper-type('InputInstructions')"/>
         <code:arguments>
            <call-template name="a:html-helper"/>
            <sequence select="$doc-output/src:reference/code:*"/>
            <choose>
               <when test="@for">
                  <variable name="param" select="src:aux-variable(generate-id())"/>
                  <code:lambda>
                     <code:parameters>
                        <code:parameter name="{$param}"/>
                     </code:parameters>
                     <code:property-reference name="{xcst:expression(@for)}">
                        <code:variable-reference name="{$param}"/>
                     </code:property-reference>
                  </code:lambda>
               </when>
               <otherwise>
                  <if test="@name">
                     <call-template name="src:expand-attribute">
                        <with-param name="attr" select="@name"/>
                     </call-template>
                  </if>
                  <if test="@value">
                     <code:expression value="{xcst:expression(@value)}"/>
                  </if>
               </otherwise>
            </choose>
            <choose>
               <when test="$hidden">
                  <code:argument name="type">
                     <code:string literal="true">hidden</code:string>
                  </code:argument>
                  <call-template name="a:html-attributes-param">
                     <with-param name="bool-attributes" select="@disabled"/>
                  </call-template>
               </when>
               <otherwise>
                  <if test="@type">
                     <code:argument name="type">
                        <call-template name="a:src.input-type">
                           <with-param name="type" select="a:input-type(@type, true())"/>
                           <with-param name="avt" select="@type"/>
                        </call-template>
                     </code:argument>
                  </if>
                  <if test="@format">
                     <code:argument name="format">
                        <call-template name="src:expand-attribute">
                           <with-param name="attr" select="@format"/>
                        </call-template>
                     </code:argument>
                  </if>
                  <call-template name="a:html-attributes-param">
                     <with-param name="merge-attributes" select="@placeholder"/>
                     <with-param name="bool-attributes" select="@disabled, @readonly, @autofocus"/>
                  </call-template>
               </otherwise>
            </choose>
         </code:arguments>
      </code:method-call>
   </template>

   <template match="a:textarea" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="$a:text-box-attributes, 'rows', 'cols'"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for">
         <with-param name="attribs" select="@name, @value"/>
      </call-template>

      <variable name="output-is-doc" select="src:output-is-doc($output)"/>
      <variable name="doc-output" select="src:doc-output(., $output)"/>

      <if test="not($output-is-doc)">
         <code:variable name="{$doc-output/src:reference/code:*/@name}">
            <code:method-call name="CastElement">
               <sequence select="src:helper-type('DocumentWriter')"/>
               <code:arguments>
                  <sequence select="$output/src:reference/code:*"/>
               </code:arguments>
            </code:method-call>
         </code:variable>
      </if>

      <code:method-call name="TextArea{'For'[current()/@for]}">
         <sequence select="a:helper-type('TextAreaInstructions')"/>
         <code:arguments>
            <call-template name="a:html-helper"/>
            <sequence select="$doc-output/src:reference/code:*"/>
            <choose>
               <when test="@for">
                  <variable name="param" select="src:aux-variable(generate-id())"/>
                  <code:lambda>
                     <code:parameters>
                        <code:parameter name="{$param}"/>
                     </code:parameters>
                     <code:property-reference name="{xcst:expression(@for)}">
                        <code:variable-reference name="{$param}"/>
                     </code:property-reference>
                  </code:lambda>
               </when>
               <otherwise>
                  <choose>
                     <when test="@name">
                        <call-template name="src:expand-attribute">
                           <with-param name="attr" select="@name"/>
                        </call-template>
                     </when>
                     <otherwise>
                        <code:string/>
                     </otherwise>
                  </choose>
                  <choose>
                     <when test="@value">
                        <code:expression value="{xcst:expression(@value)}"/>
                     </when>
                     <otherwise>
                        <code:default>
                           <sequence select="$src:object-type"/>
                        </code:default>
                     </otherwise>
                  </choose>
               </otherwise>
            </choose>
            <if test="@rows or @cols">
               <code:argument name="rows">
                  <choose>
                     <when test="@rows">
                        <call-template name="src:integer">
                           <with-param name="integer" select="xcst:integer(@rows, true())"/>
                           <with-param name="avt" select="@rows"/>
                        </call-template>
                     </when>
                     <otherwise>
                        <code:int value="2"/>
                     </otherwise>
                  </choose>
               </code:argument>
               <code:argument name="columns">
                  <choose>
                     <when test="@cols">
                        <call-template name="src:integer">
                           <with-param name="integer" select="xcst:integer(@cols, true())"/>
                           <with-param name="avt" select="@cols"/>
                        </call-template>
                     </when>
                     <otherwise>
                        <code:int value="20"/>
                     </otherwise>
                  </choose>
               </code:argument>
            </if>
            <call-template name="a:html-attributes-param">
               <with-param name="merge-attributes" select="@placeholder"/>
               <with-param name="bool-attributes" select="@disabled, @readonly, @autofocus"/>
            </call-template>
         </code:arguments>
      </code:method-call>
   </template>

   <template match="a:check-box" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="$a:input-attributes[. ne 'value'], 'checked'"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for">
         <with-param name="attribs" select="@name, @checked"/>
      </call-template>

      <variable name="output-is-doc" select="src:output-is-doc($output)"/>
      <variable name="doc-output" select="src:doc-output(., $output)"/>

      <if test="not($output-is-doc)">
         <code:variable name="{$doc-output/src:reference/code:*/@name}">
            <code:method-call name="CastElement">
               <sequence select="src:helper-type('DocumentWriter')"/>
               <code:arguments>
                  <sequence select="$output/src:reference/code:*"/>
               </code:arguments>
            </code:method-call>
         </code:variable>
      </if>

      <variable name="for-model" select="empty((@for, @name))"/>

      <code:method-call name="CheckBox{'For'[current()/@for], 'ForModel'[$for-model]}">
         <sequence select="a:helper-type('InputInstructions')"/>
         <code:arguments>
            <call-template name="a:html-helper"/>
            <sequence select="$doc-output/src:reference/code:*"/>
            <choose>
               <when test="@for">
                  <variable name="param" select="src:aux-variable(generate-id())"/>
                  <code:lambda>
                     <code:parameters>
                        <code:parameter name="{$param}"/>
                     </code:parameters>
                     <code:property-reference name="{xcst:expression(@for)}">
                        <code:variable-reference name="{$param}"/>
                     </code:property-reference>
                  </code:lambda>
               </when>
               <otherwise>
                  <if test="@name">
                     <call-template name="src:expand-attribute">
                        <with-param name="attr" select="@name"/>
                     </call-template>
                  </if>
                  <if test="@checked">
                     <code:argument name="isChecked">
                        <call-template name="src:boolean">
                           <with-param name="bool" select="xcst:boolean(@checked, true())"/>
                           <with-param name="avt" select="@checked"/>
                        </call-template>
                     </code:argument>
                  </if>
               </otherwise>
            </choose>
            <call-template name="a:html-attributes-param">
               <with-param name="bool-attributes" select="@disabled, @autofocus"/>
            </call-template>
         </code:arguments>
      </code:method-call>
   </template>

   <template match="a:radio-button" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'value'"/>
         <with-param name="optional" select="$a:input-attributes[. ne 'value'], 'checked'"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for">
         <with-param name="attribs" select="@name, @checked"/>
      </call-template>

      <variable name="output-is-doc" select="src:output-is-doc($output)"/>
      <variable name="doc-output" select="src:doc-output(., $output)"/>

      <if test="not($output-is-doc)">
         <code:variable name="{$doc-output/src:reference/code:*/@name}">
            <code:method-call name="CastElement">
               <sequence select="src:helper-type('DocumentWriter')"/>
               <code:arguments>
                  <sequence select="$output/src:reference/code:*"/>
               </code:arguments>
            </code:method-call>
         </code:variable>
      </if>

      <variable name="for-model" select="empty((@for, @name))"/>

      <code:method-call name="RadioButton{'For'[current()/@for], 'ForModel'[$for-model]}">
         <sequence select="a:helper-type('InputInstructions')"/>
         <code:arguments>
            <call-template name="a:html-helper"/>
            <sequence select="$doc-output/src:reference/code:*"/>
            <choose>
               <when test="@for">
                  <variable name="param" select="src:aux-variable(generate-id())"/>
                  <code:lambda>
                     <code:parameters>
                        <code:parameter name="{$param}"/>
                     </code:parameters>
                     <code:property-reference name="{xcst:expression(@for)}">
                        <code:variable-reference name="{$param}"/>
                     </code:property-reference>
                  </code:lambda>
               </when>
               <when test="@name">
                  <call-template name="src:expand-attribute">
                     <with-param name="attr" select="@name"/>
                  </call-template>
               </when>
            </choose>
            <code:expression value="{xcst:expression(@value)}"/>
            <if test="not(@for) and @checked">
               <code:argument name="isChecked">
                  <call-template name="src:boolean">
                     <with-param name="bool" select="xcst:boolean(@checked, true())"/>
                     <with-param name="avt" select="@checked"/>
                  </call-template>
               </code:argument>
            </if>
            <call-template name="a:html-attributes-param">
               <with-param name="bool-attributes" select="@disabled, @autofocus"/>
            </call-template>
         </code:arguments>
      </code:method-call>
   </template>

   <template match="a:anti-forgery-token" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="extension" select="true()"/>
      </call-template>

      <choose>
         <when test="$a:aspnetlib">
            <variable name="output-is-doc" select="src:output-is-doc($output)"/>
            <variable name="doc-output" select="src:doc-output(., $output)"/>
            <if test="not($output-is-doc)">
               <code:variable name="{$doc-output/src:reference/code:*/@name}">
                  <code:method-call name="CastElement">
                     <sequence select="src:helper-type('DocumentWriter')"/>
                     <code:arguments>
                        <sequence select="$output/src:reference/code:*"/>
                     </code:arguments>
                  </code:method-call>
               </code:variable>
            </if>
            <code:method-call name="GetHtml">
               <code:type-reference name="AntiForgery" namespace="System.Web.Helpers"/>
               <code:arguments>
                  <code:chain>
                     <call-template name="a:html-helper"/>
                     <code:property-reference name="ViewContext">
                        <code:chain-reference/>
                     </code:property-reference>
                     <code:property-reference name="HttpContext">
                        <code:chain-reference/>
                     </code:property-reference>
                  </code:chain>
                  <sequence select="$doc-output/src:reference/code:*"/>
               </code:arguments>
            </code:method-call>
         </when>
         <otherwise>
            <code:method-call name="WriteRaw">
               <sequence select="$output/src:reference/code:*"/>
               <code:arguments>
                  <code:chain>
                     <code:type-reference name="AntiForgery" namespace="System.Web.Helpers"/>
                     <code:method-call name="GetHtml">
                        <code:chain-reference/>
                     </code:method-call>
                     <code:method-call name="ToString">
                        <code:chain-reference/>
                     </code:method-call>
                  </code:chain>
               </code:arguments>
            </code:method-call>
         </otherwise>
      </choose>
   </template>

   <template match="a:select" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="$a:input-attributes, 'options', 'multiple'"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for">
         <with-param name="attribs" select="@name, @value"/>
      </call-template>

      <variable name="output-is-doc" select="src:output-is-doc($output)"/>
      <variable name="doc-output" select="src:doc-output(., $output)"/>

      <if test="not($output-is-doc)">
         <code:variable name="{$doc-output/src:reference/code:*/@name}">
            <code:method-call name="CastElement">
               <sequence select="src:helper-type('DocumentWriter')"/>
               <code:arguments>
                  <sequence select="$output/src:reference/code:*"/>
               </code:arguments>
            </code:method-call>
         </code:variable>
      </if>

      <variable name="for-model" select="empty((@for, @name))"/>

      <code:method-call name="Select{'For'[current()/@for], 'ForModel'[$for-model]}">
         <sequence select="a:helper-type('SelectInstructions')"/>
         <code:arguments>
            <call-template name="a:html-helper"/>
            <sequence select="$doc-output/src:reference/code:*"/>
            <if test="not($for-model)">
               <choose>
                  <when test="@for">
                     <variable name="param" select="src:aux-variable(generate-id())"/>
                     <code:lambda>
                        <code:parameters>
                           <code:parameter name="{$param}"/>
                        </code:parameters>
                        <code:property-reference name="{xcst:expression(@for)}">
                           <code:variable-reference name="{$param}"/>
                        </code:property-reference>
                     </code:lambda>
                  </when>
                  <when test="@name">
                     <call-template name="src:expand-attribute">
                        <with-param name="attr" select="@name"/>
                     </call-template>
                  </when>
               </choose>
            </if>
            <variable name="multiple" select="(@multiple/xcst:boolean(.), false())[1]"/>
            <call-template name="a:options">
               <with-param name="value" select="@value"/>
               <with-param name="multiple" select="$multiple"/>
            </call-template>
            <if test="$multiple">
               <code:argument name="multiple">
                  <code:bool value="{$multiple}"/>
               </code:argument>
            </if>
            <call-template name="a:html-attributes-param">
               <with-param name="bool-attributes" select="@disabled, @autofocus"/>
            </call-template>
         </code:arguments>
      </code:method-call>
   </template>

   <template name="a:options">
      <param name="value" as="attribute()?"/>
      <param name="multiple" select="false()"/>

      <choose>
         <when test="a:option or @options">
            <!--
               Casting of xcst:expression avoids turning into a dynamic object when one of the arguments is dynamic.
               A long method chain on a dynamic object hurts performance.
            -->
            <code:chain>
               <sequence select="a:helper-type('OptionList')"/>
               <choose>
                  <when test="a:option">
                     <code:method-call name="FromStaticList">
                        <code:chain-reference/>
                        <code:arguments>
                           <code:int value="{count(a:option)}"/>
                        </code:arguments>
                     </code:method-call>
                  </when>
                  <otherwise>
                     <code:method-call name="Create">
                        <code:chain-reference/>
                     </code:method-call>
                  </otherwise>
               </choose>
               <if test="$value">
                  <code:method-call name="WithSelectedValue">
                     <call-template name="src:line-number"/>
                     <code:chain-reference/>
                     <code:arguments>
                        <code:cast>
                           <sequence select="$src:object-type"/>
                           <code:expression value="{xcst:expression($value)}"/>
                        </code:cast>
                        <if test="$multiple">
                           <code:argument name="multiple">
                              <code:bool value="{$multiple}"/>
                           </code:argument>
                        </if>
                     </code:arguments>
                  </code:method-call>
               </if>
               <for-each select="a:option">
                  <call-template name="xcst:validate-attribs">
                     <with-param name="optional" select="'value', 'selected', 'disabled'"/>
                     <with-param name="extension" select="true()"/>
                  </call-template>
                  <code:method-call name="AddStaticOption">
                     <call-template name="src:line-number"/>
                     <code:chain-reference/>
                     <code:arguments>
                        <if test="@value">
                           <code:argument name="value">
                              <code:cast>
                                 <sequence select="$src:object-type"/>
                                 <code:expression value="{xcst:expression(@value)}"/>
                              </code:cast>
                           </code:argument>
                        </if>
                        <code:argument name="text">
                           <call-template name="src:simple-content"/>
                        </code:argument>
                        <if test="@selected">
                           <code:argument name="selected">
                              <call-template name="src:boolean">
                                 <with-param name="bool" select="xcst:boolean(@selected, true())"/>
                                 <with-param name="avt" select="@selected"/>
                              </call-template>
                           </code:argument>
                        </if>
                        <if test="@disabled">
                           <code:argument name="disabled">
                              <call-template name="src:boolean">
                                 <with-param name="bool" select="xcst:boolean(@disabled, true())"/>
                                 <with-param name="avt" select="@disabled"/>
                              </call-template>
                           </code:argument>
                        </if>
                     </code:arguments>
                  </code:method-call>
               </for-each>
               <if test="@options">
                  <code:method-call name="ConcatDynamicList">
                     <call-template name="src:line-number"/>
                     <code:chain-reference/>
                     <code:arguments>
                        <!-- Don't cast expression, behavior depends on overload resolution -->
                        <code:expression value="{xcst:expression(@options)}"/>
                     </code:arguments>
                  </code:method-call>
               </if>
            </code:chain>
         </when>
         <otherwise>
            <code:default>
               <code:type-reference name="IEnumerable" namespace="System.Collections.Generic">
                  <code:type-arguments>
                     <code:type-reference name="SelectListItem" namespace="System.Web.Mvc"/>
                  </code:type-arguments>
               </code:type-reference>
            </code:default>
         </otherwise>
      </choose>
   </template>

   <template match="a:label" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="$a:html-attributes, 'for', 'name', 'text'"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for"/>

      <variable name="output-is-doc" select="src:output-is-doc($output)"/>
      <variable name="doc-output" select="src:doc-output(., $output)"/>

      <if test="not($output-is-doc)">
         <code:variable name="{$doc-output/src:reference/code:*/@name}">
            <code:method-call name="CastElement">
               <sequence select="src:helper-type('DocumentWriter')"/>
               <code:arguments>
                  <sequence select="$output/src:reference/code:*"/>
               </code:arguments>
            </code:method-call>
         </code:variable>
      </if>

      <variable name="for-model" select="empty((@for, @name))"/>

      <code:method-call name="Label{'For'[current()/@for], 'ForModel'[$for-model]}">
         <sequence select="a:helper-type('LabelInstructions')"/>
         <code:arguments>
            <call-template name="a:html-helper"/>
            <sequence select="$doc-output/src:reference/code:*"/>
            <if test="not($for-model)">
               <choose>
                  <when test="@for">
                     <variable name="param" select="src:aux-variable(generate-id())"/>
                     <code:lambda>
                        <code:parameters>
                           <code:parameter name="{$param}"/>
                        </code:parameters>
                        <code:property-reference name="{xcst:expression(@for)}">
                           <code:variable-reference name="{$param}"/>
                        </code:property-reference>
                     </code:lambda>
                  </when>
                  <when test="@name">
                     <call-template name="src:expand-attribute">
                        <with-param name="attr" select="@name"/>
                     </call-template>
                  </when>
               </choose>
            </if>
            <if test="@text">
               <code:argument name="labelText">
                  <call-template name="src:expand-attribute">
                     <with-param name="attr" select="@text"/>
                  </call-template>
               </code:argument>
            </if>
            <call-template name="a:html-attributes-param"/>
         </code:arguments>
      </code:method-call>
   </template>

   <template match="a:validation-summary" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="$a:html-attributes, 'include-member-errors', 'message'"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <variable name="output-is-doc" select="src:output-is-doc($output)"/>
      <variable name="doc-output" select="src:doc-output(., $output)"/>

      <if test="not($output-is-doc)">
         <code:variable name="{$doc-output/src:reference/code:*/@name}">
            <code:method-call name="CastElement">
               <sequence select="src:helper-type('DocumentWriter')"/>
               <code:arguments>
                  <sequence select="$output/src:reference/code:*"/>
               </code:arguments>
            </code:method-call>
         </code:variable>
      </if>

      <code:method-call name="ValidationSummary">
         <sequence select="a:helper-type('ValidationInstructions')"/>
         <code:arguments>
            <call-template name="a:html-helper"/>
            <sequence select="$doc-output/src:reference/code:*"/>
            <if test="@include-member-errors">
               <code:argument name="includePropertyErrors">
                  <call-template name="src:boolean">
                     <with-param name="bool" select="xcst:boolean(@include-member-errors, true())"/>
                     <with-param name="avt" select="@include-member-errors"/>
                  </call-template>
               </code:argument>
            </if>
            <code:argument name="message">
               <choose>
                  <when test="@message">
                     <call-template name="src:expand-attribute">
                        <with-param name="attr" select="@message"/>
                     </call-template>
                  </when>
                  <otherwise>
                     <code:default>
                        <code:type-reference name="String" namespace="System"/>
                     </code:default>
                  </otherwise>
               </choose>
            </code:argument>
            <call-template name="a:html-attributes-param"/>
         </code:arguments>
      </code:method-call>
   </template>

   <template match="a:validation-message" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="$a:html-attributes, 'for', 'name', 'message'"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for"/>

      <variable name="output-is-doc" select="src:output-is-doc($output)"/>
      <variable name="doc-output" select="src:doc-output(., $output)"/>

      <if test="not($output-is-doc)">
         <code:variable name="{$doc-output/src:reference/code:*/@name}">
            <code:method-call name="CastElement">
               <sequence select="src:helper-type('DocumentWriter')"/>
               <code:arguments>
                  <sequence select="$output/src:reference/code:*"/>
               </code:arguments>
            </code:method-call>
         </code:variable>
      </if>

      <code:method-call name="ValidationMessage{'For'[current()/@for]}">
         <sequence select="a:helper-type('ValidationInstructions')"/>
         <code:arguments>
            <call-template name="a:html-helper"/>
            <sequence select="$doc-output/src:reference/code:*"/>
            <choose>
               <when test="@for">
                  <variable name="param" select="src:aux-variable(generate-id())"/>
                  <code:lambda>
                     <code:parameters>
                        <code:parameter name="{$param}"/>
                     </code:parameters>
                     <code:property-reference name="{xcst:expression(@for)}">
                        <code:variable-reference name="{$param}"/>
                     </code:property-reference>
                  </code:lambda>
               </when>
               <when test="@name">
                  <call-template name="src:expand-attribute">
                     <with-param name="attr" select="@name"/>
                  </call-template>
               </when>
               <otherwise>
                  <code:string/>
               </otherwise>
            </choose>
            <choose>
               <when test="@message">
                  <call-template name="src:expand-attribute">
                     <with-param name="attr" select="@message"/>
                  </call-template>
               </when>
               <otherwise>
                  <code:default>
                     <code:type-reference name="String" namespace="System"/>
                  </code:default>
               </otherwise>
            </choose>
            <call-template name="a:html-attributes-param"/>
         </code:arguments>
      </code:method-call>
   </template>

   <!--
      ## Templates
   -->

   <template match="a:editor | a:display" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <variable name="editor" select="self::a:editor"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'for', 'name', 'template', 'field-name', 'attributes', 'with-params', 'options', ('autofocus', 'disabled', 'readonly')[$editor]"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for"/>

      <variable name="output-is-doc" select="src:output-is-doc($output)"/>
      <variable name="doc-output" select="src:doc-output(., $output)"/>

      <if test="not($output-is-doc)">
         <code:variable name="{$doc-output/src:reference/code:*/@name}">
            <code:method-call name="CastElement">
               <sequence select="src:helper-type('DocumentWriter')"/>
               <code:arguments>
                  <sequence select="$output/src:reference/code:*"/>
               </code:arguments>
            </code:method-call>
         </code:variable>
      </if>

      <variable name="for-model" select="empty((@for, @name))"/>

      <code:method-call name="{if ($editor) then 'Editor' else 'Display'}{'For'[current()/@for], 'ForModel'[$for-model]}">
         <sequence select="a:helper-type(concat((if ($editor) then 'Editor' else 'Display'), 'Instructions'))"/>
         <code:arguments>
            <call-template name="a:html-helper"/>
            <sequence select="$doc-output/src:reference/code:*"/>
            <if test="not($for-model)">
               <choose>
                  <when test="@for">
                     <variable name="param" select="src:aux-variable(generate-id())"/>
                     <code:lambda>
                        <code:parameters>
                           <code:parameter name="{$param}"/>
                        </code:parameters>
                        <code:property-reference name="{xcst:expression(@for)}">
                           <code:variable-reference name="{$param}"/>
                        </code:property-reference>
                     </code:lambda>
                  </when>
                  <when test="@name">
                     <call-template name="src:expand-attribute">
                        <with-param name="attr" select="@name"/>
                     </call-template>
                  </when>
               </choose>
            </if>
            <code:argument name="templateName">
               <choose>
                  <when test="@template">
                     <call-template name="src:expand-attribute">
                        <with-param name="attr" select="@template"/>
                     </call-template>
                  </when>
                  <otherwise>
                     <code:null/>
                  </otherwise>
               </choose>
            </code:argument>
            <if test="@field-name">
               <code:argument name="htmlFieldName">
                  <call-template name="src:expand-attribute">
                     <with-param name="attr" select="@field-name"/>
                  </call-template>
               </code:argument>
            </if>
            <call-template name="a:editor-additional-view-data"/>
         </code:arguments>
      </code:method-call>
   </template>

   <template name="a:editor-additional-view-data">
      <variable name="editor" select="self::a:editor"/>
      <variable name="boolean-attribs" select="(@autofocus, @disabled, @readonly)[$editor]"/>
      <variable name="inits" as="element()*">
         <if test="$boolean-attribs">
            <code:collection-initializer>
               <code:string literal="true">htmlAttributes</code:string>
               <call-template name="a:html-attributes-param">
                  <with-param name="bool-attributes" select="$boolean-attribs"/>
                  <with-param name="class" select="()"/>
                  <with-param name="omit-param" select="true()"/>
               </call-template>
            </code:collection-initializer>
         </if>
         <for-each select="@attributes[not($boolean-attribs)], a:with-options, .[@options], a:member-template">
            <apply-templates select="." mode="a:editor-additional-view-data"/>
         </for-each>
      </variable>
      <variable name="with-params" select="@with-params/xcst:expression(.)"/>
      <if test="$with-params or $inits">
         <code:argument name="additionalViewData">
            <choose>
               <when test="$with-params and empty($inits)">
                  <code:expression value="{$with-params}"/>
               </when>
               <otherwise>
                  <code:new-object>
                     <code:type-reference name="Dictionary" namespace="System.Collections.Generic">
                        <code:type-arguments>
                           <code:type-reference name="String" namespace="System"/>
                           <code:type-reference name="Object" namespace="System"/>
                        </code:type-arguments>
                     </code:type-reference>
                     <if test="$with-params">
                        <code:arguments>
                           <code:method-call name="ObjectToDictionary">
                              <code:type-reference name="HtmlHelper" namespace="System.Web.Mvc"/>
                              <code:arguments>
                                 <code:expression value="{$with-params}"/>
                              </code:arguments>
                           </code:method-call>
                        </code:arguments>
                     </if>
                     <if test="$inits">
                        <code:collection-initializer>
                           <sequence select="$inits"/>
                        </code:collection-initializer>
                     </if>
                  </code:new-object>
               </otherwise>
            </choose>
         </code:argument>
      </if>
   </template>

   <template match="@attributes" mode="a:editor-additional-view-data">
      <code:collection-initializer>
         <code:string literal="true">htmlAttributes</code:string>
         <code:expression value="{xcst:expression(.)}"/>
      </code:collection-initializer>
   </template>

   <template match="a:with-options | a:*[@options]" mode="a:editor-additional-view-data">

      <if test="self::a:with-options">
         <call-template name="xcst:validate-attribs">
            <with-param name="optional" select="'for', 'name', 'options'"/>
            <with-param name="extension" select="true()"/>
         </call-template>
         <call-template name="a:validate-for"/>
      </if>

      <variable name="for" select="@for or self::a:with-options/../@for"/>
      <variable name="name" select="@name or self::a:with-options/../@name"/>
      <variable name="for-model" select="not($for or $name)"/>

      <code:collection-initializer>
         <code:add>
            <code:string literal="true">
               <value-of select="concat(src:aux-variable('options'), ':')"/>
            </code:string>
            <code:method-call name="Name{'For'[$for], 'ForModel'[$for-model]}">
               <sequence select="a:helper-type('InputInstructions')"/>
               <code:arguments>
                  <call-template name="a:html-helper"/>
                  <choose>
                     <when test="$for">
                        <variable name="param" select="src:aux-variable(generate-id())"/>
                        <code:lambda>
                           <code:parameters>
                              <code:parameter name="{$param}"/>
                           </code:parameters>
                           <code:chain>
                              <code:variable-reference name="{$param}"/>
                              <if test="self::a:with-options/../@for">
                                 <code:property-reference name="{../xcst:expression(@for)}">
                                    <code:chain-reference/>
                                 </code:property-reference>
                              </if>
                              <if test="@for">
                                 <code:property-reference name="{xcst:expression(@for)}">
                                    <code:chain-reference/>
                                 </code:property-reference>
                              </if>
                           </code:chain>
                        </code:lambda>
                     </when>
                     <when test="$name">
                        <variable name="parts" as="element()+">
                           <if test="self::a:with-options/../@name">
                              <call-template name="src:expand-attribute">
                                 <with-param name="attr" select="../@name"/>
                              </call-template>
                           </if>
                           <if test="@name">
                              <call-template name="src:expand-attribute">
                                 <with-param name="attr" select="@name"/>
                              </call-template>
                           </if>
                        </variable>
                        <choose>
                           <when test="count($parts) eq 1">
                              <sequence select="$parts"/>
                           </when>
                           <otherwise>
                              <code:method-call name="Join">
                                 <code:type-reference name="String" namespace="System"/>
                                 <code:arguments>
                                    <code:string literal="true">.</code:string>
                                    <sequence select="$parts"/>
                                 </code:arguments>
                              </code:method-call>
                           </otherwise>
                        </choose>
                     </when>
                  </choose>
               </code:arguments>
            </code:method-call>
         </code:add>
         <call-template name="a:options"/>
      </code:collection-initializer>
   </template>

   <template match="a:member-template" mode="a:editor-additional-view-data">

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'helper-name'"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <variable name="meta" as="element()">
         <xcst:delegate/>
      </variable>

      <variable name="new-output" select="src:template-output($meta, .)"/>

      <variable name="new-helper" as="element()">
         <code:variable-reference name="{(@helper-name/xcst:name(.), concat(src:aux-variable('model_helper'), '_', generate-id()))[1]}"/>
      </variable>

      <code:collection-initializer>
         <code:string literal="true">
            <value-of select="src:aux-variable('member_template')"/>
         </code:string>
         <code:new-object>
            <code:type-reference name="Action" namespace="System">
               <code:type-arguments>
                  <code:type-reference name="HtmlHelper" namespace="System.Web.Mvc"/>
                  <sequence select="$new-output/code:type-reference"/>
               </code:type-arguments>
            </code:type-reference>
            <code:arguments>
               <code:lambda void="true">
                  <code:parameters>
                     <code:parameter name="{$new-helper/@name}"/>
                     <code:parameter name="{$new-output/src:reference/code:*/@name}"/>
                  </code:parameters>
                  <code:block>
                     <call-template name="src:sequence-constructor">
                        <with-param name="output" select="$new-output" tunnel="yes"/>
                        <with-param name="a:html-helper" select="$new-helper" tunnel="yes"/>
                     </call-template>
                  </code:block>
               </code:lambda>
            </code:arguments>
         </code:new-object>
      </code:collection-initializer>
   </template>

   <!--
      ## Metadata
   -->

   <template match="c:type | c:member" mode="src:type-attribute-extra">
      <next-match/>
      <variable name="excluded" select="c:member[@a:skip-binding[xcst:boolean(.)]]/xcst:name(@name)"/>
      <if test="exists($excluded)">
         <code:attribute>
            <code:type-reference name="Bind" namespace="System.Web.Mvc"/>
            <code:initializer>
               <code:member-initializer name="Exclude">
                  <code:string literal="true">
                     <value-of select="string-join($excluded, ',')"/>
                  </code:string>
               </code:member-initializer>
            </code:initializer>
         </code:attribute>
      </if>
   </template>

   <template match="c:member" mode="src:member-attribute-extra">
      <next-match/>
      <apply-templates select="@display, @a:*" mode="a:src.member-attribute-extra"/>
   </template>

   <template match="@display" mode="a:src.member-attribute-extra">
      <next-match/>
      <variable name="display" select="xcst:non-string(.)"/>
      <variable name="scaffold" select="
         if ($display = ('view-only', 'edit-only', 'hidden')) then true()
         else xcst:boolean(.)"/>
      <if test="$scaffold">
         <code:attribute line-hidden="true">
            <code:type-reference name="ShowFor" namespace="Xcst.Web.Mvc"/>
            <code:initializer>
               <code:member-initializer name="Display">
                  <code:bool value="{$display ne 'edit-only'}"/>
               </code:member-initializer>
               <code:member-initializer name="Edit">
                  <code:bool value="{$display ne 'view-only'}"/>
               </code:member-initializer>
            </code:initializer>
         </code:attribute>
         <if test="$display eq 'hidden'">
            <code:attribute>
               <code:type-reference name="HiddenInput" namespace="System.Web.Mvc"/>
               <code:initializer>
                  <code:member-initializer name="DisplayValue">
                     <code:bool value="false"/>
                  </code:member-initializer>
               </code:initializer>
            </code:attribute>
         </if>
      </if>
   </template>

   <template match="@a:file-extensions" mode="a:src.member-attribute-extra">
      <code:attribute>
         <code:type-reference name="FileExtensions" namespace="Xcst.Web.Mvc"/>
         <code:arguments>
            <code:string verbatim="true">
               <value-of select="xcst:non-string(.)"/>
            </code:string>
         </code:arguments>
         <code:initializer>
            <call-template name="src:validation-arguments">
               <with-param name="name" select="node-name(.)"/>
            </call-template>
         </code:initializer>
      </code:attribute>
   </template>

   <template match="@a:file-max-length" mode="a:src.member-attribute-extra">
      <code:attribute>
         <code:type-reference name="FileMaxLength" namespace="Xcst.Web.Mvc"/>
         <code:arguments>
            <code:int value="{xcst:integer(.)}"/>
         </code:arguments>
         <code:initializer>
            <call-template name="src:validation-arguments">
               <with-param name="name" select="node-name(.)"/>
            </call-template>
         </code:initializer>
      </code:attribute>
   </template>

   <template match="@a:file-extensions-message | @a:file-max-length-message | @a:skip-binding" mode="a:src.member-attribute-extra"/>

   <template match="@a:*" mode="a:src.member-attribute-extra">
      <sequence select="error((), concat('Attribute ''a:', local-name(), ''' is not allowed on element ', name(..)), src:error-object(.))"/>
   </template>

   <template match="a:display-name" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>
      <param name="src:current-mode" as="xs:QName" required="yes" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'for', 'name'"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for"/>

      <variable name="statement" select="$src:current-mode eq xs:QName('src:statement')"/>
      <variable name="for-model" select="empty((@for, @name))"/>

      <variable name="expr" as="element()">
         <code:method-call name="DisplayName{'For'[current()/@for], 'ForModel'[$for-model]}">
            <sequence select="a:helper-type('MetadataInstructions')"/>
            <code:arguments>
               <call-template name="a:html-helper"/>
               <if test="not($for-model)">
                  <choose>
                     <when test="@for">
                        <variable name="param" select="src:aux-variable(generate-id())"/>
                        <code:lambda>
                           <code:parameters>
                              <code:parameter name="{$param}"/>
                           </code:parameters>
                           <code:property-reference name="{xcst:expression(@for)}">
                              <code:variable-reference name="{$param}"/>
                           </code:property-reference>
                        </code:lambda>
                     </when>
                     <when test="@name">
                        <call-template name="src:expand-attribute">
                           <with-param name="attr" select="@name"/>
                        </call-template>
                     </when>
                  </choose>
               </if>
            </code:arguments>
         </code:method-call>
      </variable>

      <choose>
         <when test="$statement">
            <code:method-call name="WriteString">
               <sequence select="$output/src:reference/code:*"/>
               <code:arguments>
                  <sequence select="$expr"/>
               </code:arguments>
            </code:method-call>
         </when>
         <otherwise>
            <sequence select="$expr"/>
         </otherwise>
      </choose>
   </template>

   <template match="a:display-name" mode="xcst:extension-instruction">
      <xcst:instruction expression="true">
         <code:type-reference name="String" namespace="System"/>
      </xcst:instruction>
   </template>

   <template match="a:display-text" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>
      <param name="src:current-mode" as="xs:QName" required="yes" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'for', 'name'"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for"/>

      <variable name="statement" select="$src:current-mode eq xs:QName('src:statement')"/>

      <code:method-call name="Display{if ($statement) then 'Text' else 'String'}{'For'[current()/@for]}">
         <sequence select="a:helper-type('MetadataInstructions')"/>
         <code:arguments>
            <call-template name="a:html-helper"/>
            <if test="$statement">
               <sequence select="$output/src:reference/code:*"/>
            </if>
            <choose>
               <when test="@for">
                  <variable name="param" select="src:aux-variable(generate-id())"/>
                  <code:lambda>
                     <code:parameters>
                        <code:parameter name="{$param}"/>
                     </code:parameters>
                     <code:property-reference name="{xcst:expression(@for)}">
                        <code:variable-reference name="{$param}"/>
                     </code:property-reference>
                  </code:lambda>
               </when>
               <when test="@name">
                  <call-template name="src:expand-attribute">
                     <with-param name="attr" select="@name"/>
                  </call-template>
               </when>
               <otherwise>
                  <code:string/>
               </otherwise>
            </choose>
         </code:arguments>
      </code:method-call>
   </template>

   <template match="a:display-text" mode="xcst:extension-instruction">
      <xcst:instruction expression="true">
         <code:type-reference name="String" namespace="System"/>
      </xcst:instruction>
   </template>

   <!--
      ## Models
   -->

   <template match="a:model" mode="src:extension-instruction">

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'value', 'as', 'field-prefix', 'helper-name', 'with-params'"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <if test="not(@value or @as)">
         <sequence select="error((), 'Element must have either @value or @as attributes.', src:error-object(.))"/>
      </if>

      <variable name="new-helper" as="element()">
         <code:variable-reference name="{(@helper-name/xcst:name(.), concat(src:aux-variable('html_helper'), '_', generate-id()))[1]}"/>
      </variable>

      <variable name="type" as="element()?">
         <if test="@as">
            <code:type-reference name="{xcst:type(@as)}"/>
         </if>
      </variable>

      <code:block>
         <code:variable name="{$new-helper/@name}">
            <code:method-call name="ForModel">
               <sequence select="a:helper-type('HtmlHelperFactory')"/>
               <if test="$type">
                  <code:type-arguments>
                     <sequence select="$type"/>
                  </code:type-arguments>
               </if>
               <code:arguments>
                  <call-template name="a:html-helper"/>
                  <choose>
                     <when test="@value">
                        <code:expression value="{xcst:expression(@value)}"/>
                     </when>
                     <otherwise>
                        <code:default>
                           <sequence select="($type, $src:object-type)[1]"/>
                        </code:default>
                     </otherwise>
                  </choose>
                  <if test="@field-prefix">
                     <code:argument name="htmlFieldPrefix">
                        <call-template name="src:expand-attribute">
                           <with-param name="attr" select="@field-prefix"/>
                        </call-template>
                     </code:argument>
                  </if>
                  <if test="@with-params">
                     <code:argument name="additionalViewData">
                        <code:expression value="{xcst:expression(@with-params)}"/>
                     </code:argument>
                  </if>
               </code:arguments>
            </code:method-call>
         </code:variable>
         <call-template name="src:sequence-constructor">
            <with-param name="a:html-helper" select="$new-helper" tunnel="yes"/>
         </call-template>
      </code:block>
   </template>

   <!--
      ## Infrastructure
   -->

   <template match="/" mode="src:main">
      <next-match>
         <with-param name="a:directives" tunnel="yes">
            <apply-templates select="processing-instruction()" mode="a:directive"/>
         </with-param>
      </next-match>
   </template>

   <template match="processing-instruction(inherits) | processing-instruction(model)" mode="a:directive">
      <if test="preceding-sibling::processing-instruction()[name() eq name(current())]">
         <sequence select="error((), concat('Only one ''', name(), ''' directive is allowed.'), src:error-object(.))"/>
      </if>
      <if test="preceding-sibling::processing-instruction()[name() = ('inherits', 'model')]">
         <sequence select="error((), '''inherits'' and ''model'' directives are mutually exclusive.', src:error-object(.))"/>
      </if>
      <element name="{name()}" namespace="">
         <value-of select="xcst:non-string(.)"/>
      </element>
   </template>

   <template match="processing-instruction(sessionstate)" mode="a:directive">
      <if test="preceding-sibling::processing-instruction()[name() eq name(current())]">
         <sequence select="error((), concat('Only one ''', name(), ''' directive is allowed.'), src:error-object(.))"/>
      </if>
      <variable name="value" select="xcst:non-string(.)"/>
      <variable name="allowed" select="'Default', 'Required', 'ReadOnly', 'Disabled'"/>
      <if test="not($value = $allowed)">
         <sequence select="error((), concat('Invalid value for ''', name(), ''' directive. Must be one of (', string-join($allowed, '|'), ').'), src:error-object(.))"/>
      </if>
      <element name="{name()}" namespace="">
         <value-of select="$value"/>
      </element>
   </template>

   <template match="c:module | c:package" mode="src:import-namespace-extra">
      <param name="package-manifest" required="yes" tunnel="yes"/>

      <next-match/>
      <if test="$a:href-fn and a:href-function-base(.)">
         <code:import static="true">
            <code:type-reference name="{a:functions-type-name(.)}">
               <sequence select="$package-manifest/code:type-reference"/>
            </code:type-reference>
         </code:import>
      </if>
   </template>

   <template match="c:module | c:package" mode="src:base-types" as="element(code:type-reference)*">
      <param name="a:directives" as="node()?" tunnel="yes"/>

      <variable name="base-types" select="
         if (exists($src:base-types)) then $src:base-types
         else $a:page-type"/>

      <choose>
         <when test="$a:directives/inherits">
            <code:type-reference name="{$a:directives/inherits}"/>
         </when>
         <!--
            First condition is always true for pages.
            Second condition can be true for packages in App_Code.
         -->
         <when test="exists($src:base-types) or (exists($a:page-type) and $a:directives/model)">
            <choose>
               <!--
                  Second condition is always false for packages in App_Code.
               -->
               <when test="$a:directives/model or $a:default-model or $a:default-model-dynamic">
                  <code:type-reference>
                     <sequence select="$base-types[1]/@*"/>
                     <code:type-arguments>
                        <choose>
                           <when test="$a:directives/model">
                              <code:type-reference name="{$a:directives/model}"/>
                           </when>
                           <when test="$a:default-model">
                              <code:type-reference name="{$a:default-model}"/>
                           </when>
                           <when test="$a:default-model-dynamic">
                              <code:type-reference name="Object" namespace="System" dynamic="true"/>
                           </when>
                        </choose>
                     </code:type-arguments>
                  </code:type-reference>
               </when>
               <otherwise>
                  <sequence select="$base-types[1]"/>
               </otherwise>
            </choose>
         </when>
      </choose>
      <sequence select="$base-types[position() gt 1]"/>
      <if test="$a:directives/sessionstate">
         <code:type-reference name="ISessionStateAware" namespace="Xcst.Web"/>
      </if>
   </template>

   <template match="c:module | c:package" mode="src:infrastructure-extra">
      <param name="modules" tunnel="yes"/>
      <param name="a:directives" as="node()?" tunnel="yes"/>

      <next-match/>

      <variable name="module-pos" select="
         for $pos in (1 to count($modules))
         return if ($modules[$pos] is current()) then $pos else ()"/>
      <variable name="principal-module" select="$module-pos eq count($modules)"/>

      <if test="$a:aspnetlib
         and $a:directives/sessionstate
         and $principal-module">

         <code:property name="SessionStateBehavior">
            <code:type-reference name="SessionStateBehavior" namespace="System.Web.SessionState"/>
            <code:implements-interface>
               <code:type-reference name="ISessionStateAware" namespace="Xcst.Web"/>
            </code:implements-interface>
            <code:getter>
               <code:block>
                  <code:return>
                     <code:field-reference name="{$a:directives/sessionstate}">
                        <code:type-reference name="SessionStateBehavior" namespace="System.Web.SessionState"/>
                     </code:field-reference>
                  </code:return>
               </code:block>
            </code:getter>
         </code:property>
      </if>

      <if test="$a:href-fn">
         <variable name="relative-uri" select="a:href-function-base(.)"/>

         <if test="$relative-uri">
            <variable name="functions-type" select="a:functions-type-name(.)"/>

            <code:type name="{$functions-type}" extensibility="static" visibility="internal">
               <code:members>
                  <code:field name="BasePath" readonly="true" extensibility="static">
                     <code:type-reference name="String" namespace="System"/>
                     <code:expression>
                        <code:method-call name="ToAbsolute">
                           <code:type-reference name="VirtualPathUtility" namespace="System.Web"/>
                           <code:arguments>
                              <code:string verbatim="true">
                                 <value-of select="concat('~/', $relative-uri)"/>
                              </code:string>
                           </code:arguments>
                        </code:method-call>
                     </code:expression>
                  </code:field>
                  <code:method name="Href" extensibility="static" visibility="public">
                     <code:type-reference name="String" namespace="System"/>
                     <variable name="path-ref" as="element()">
                        <code:variable-reference name="path"/>
                     </variable>
                     <variable name="pathParts-ref" as="element()">
                        <code:variable-reference name="pathParts"/>
                     </variable>
                     <code:parameters>
                        <code:parameter name="{$path-ref/@name}">
                           <code:type-reference name="String" namespace="System"/>
                        </code:parameter>
                        <code:parameter name="{$pathParts-ref/@name}" params="true">
                           <code:type-reference array-dimensions="1">
                              <sequence select="$src:object-type"/>
                           </code:type-reference>
                        </code:parameter>
                     </code:parameters>
                     <code:block>
                        <code:return>
                           <code:method-call name="GenerateClientUrl">
                              <sequence select="a:helper-type('UrlUtil')"/>
                              <code:arguments>
                                 <code:field-reference name="BasePath">
                                    <code:type-reference name="{$functions-type}"/>
                                 </code:field-reference>
                                 <sequence select="$path-ref, $pathParts-ref"/>
                              </code:arguments>
                           </code:method-call>
                        </code:return>
                     </code:block>
                  </code:method>
               </code:members>
            </code:type>
         </if>
      </if>
   </template>

   <function name="a:functions-type-name">
      <param name="module" as="element()"/>

      <sequence select="concat('__xcst_functions_', generate-id($module))"/>
   </function>

   <function name="a:href-function-base" as="xs:anyURI?">
      <param name="module" as="element()"/>

      <variable name="module-uri" select="document-uri(root($module))"/>
      <variable name="relative-uri" select="src:make-relative-uri($a:application-uri, $module-uri)"/>
      <sequence select="$relative-uri[not(starts-with(., '..'))]"/>
   </function>

   <!--
      ## Helpers
   -->

   <template name="a:validate-for">
      <param name="attribs" select="@name" as="attribute()*"/>

      <if test="@for and $attribs">
         <sequence select="error((), concat('@for and @', $attribs[1]/local-name(), ' attributes are mutually exclusive.'), src:error-object(.))"/>
      </if>
   </template>

   <function name="a:input-type" as="xs:string?">
      <param name="node" as="node()"/>
      <param name="avt" as="xs:boolean"/>

      <variable name="string" select="xcst:non-string($node)"/>
      <sequence select="
         if ($avt and xcst:is-value-template($node)) then ()
         else $string
      "/>
   </function>

   <template name="a:src.input-type">
      <param name="type" as="xs:string?"/>
      <param name="avt" as="attribute()?"/>

      <choose>
         <when test="$type instance of xs:string">
            <code:string verbatim="true">
               <value-of select="$type"/>
            </code:string>
         </when>
         <otherwise>
            <call-template name="src:expand-attribute">
               <with-param name="attr" select="$avt"/>
            </call-template>
         </otherwise>
      </choose>
   </template>

   <template name="a:html-helper">
      <param name="a:html-helper" as="element()?" tunnel="yes"/>

      <choose>
         <when test="$a:html-helper">
            <sequence select="$a:html-helper"/>
         </when>
         <otherwise>
            <code:property-reference name="Html">
               <code:cast>
                  <code:type-reference name="XcstViewPage" namespace="Xcst.Web.Mvc"/>
                  <code:property-reference name="TopLevelPackage">
                     <sequence select="$src:context-field/src:reference/code:*"/>
                  </code:property-reference>
               </code:cast>
            </code:property-reference>
         </otherwise>
      </choose>
   </template>

   <template name="a:html-attributes-param">
      <param name="attributes" select="@attributes" as="attribute()?"/>
      <param name="class" select="@class" as="attribute()?"/>
      <param name="merge-attributes" as="attribute()*"/>
      <param name="bool-attributes" as="attribute()*"/>
      <param name="omit-param" select="false()"/>

      <if test="exists(($attributes, $class, $merge-attributes, $bool-attributes))">
         <variable name="expr" as="element()">
            <code:chain>
               <code:new-object>
                  <sequence select="a:helper-type('HtmlAttributeDictionary')"/>
                  <if test="$attributes">
                     <code:arguments>
                        <code:expression value="{xcst:expression($attributes)}"/>
                     </code:arguments>
                  </if>
               </code:new-object>
               <if test="$class">
                  <code:method-call name="AddCssClass">
                     <code:chain-reference/>
                     <code:arguments>
                        <call-template name="src:expand-attribute">
                           <with-param name="attr" select="$class"/>
                        </call-template>
                     </code:arguments>
                  </code:method-call>
               </if>
               <for-each select="$merge-attributes">
                  <code:method-call name="MergeAttribute">
                     <code:chain-reference/>
                     <code:arguments>
                        <code:string literal="true">
                           <value-of select="local-name()"/>
                        </code:string>
                        <call-template name="src:expand-attribute">
                           <with-param name="attr" select="."/>
                        </call-template>
                     </code:arguments>
                  </code:method-call>
               </for-each>
               <for-each select="$bool-attributes">
                  <code:method-call name="MergeBoolean">
                     <code:chain-reference/>
                     <code:arguments>
                        <code:string literal="true">
                           <value-of select="local-name()"/>
                        </code:string>
                        <call-template name="src:boolean">
                           <with-param name="bool" select="xcst:boolean(., true())"/>
                           <with-param name="avt" select="."/>
                        </call-template>
                     </code:arguments>
                  </code:method-call>
               </for-each>
            </code:chain>
         </variable>

         <choose>
            <when test="not($omit-param)">
               <code:argument name="htmlAttributes">
                  <sequence select="$expr"/>
               </code:argument>
            </when>
            <otherwise>
               <sequence select="$expr"/>
            </otherwise>
         </choose>
      </if>
   </template>

   <function name="a:helper-type" as="element(code:type-reference)">
      <param name="helper" as="xs:string"/>

      <code:type-reference name="{$helper}" namespace="Xcst.Web.Runtime"/>
   </function>

</stylesheet>

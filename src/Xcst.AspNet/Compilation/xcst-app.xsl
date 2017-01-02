﻿<?xml version="1.0" encoding="utf-8"?>
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
   xmlns:src="http://maxtoroq.github.io/XCST/compiled"
   xmlns:xcst="http://maxtoroq.github.io/XCST/syntax">

   <param name="a:application-uri" as="xs:anyURI"/>

   <variable name="a:html-attributes" select="'html-class', 'html-attributes'"/>
   <variable name="a:input-attributes" select="'for', 'name', 'value', $a:html-attributes"/>

   <!--
      ## Forms
   -->

   <template match="a:text-box" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="$a:input-attributes, 'format', 'html-type', 'html-placeholder'"/>
         <with-param name="required" select="()"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for">
         <with-param name="attribs" select="@name, @value"/>
      </call-template>

      <variable name="expr">
         <value-of select="a:fully-qualified-helper-html('InputExtensions')"/>
         <text>.TextBox</text>
         <if test="@for">For</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <value-of select="$output"/>
         <text>, </text>
         <choose>
            <when test="@for">
               <variable name="param" select="src:aux-variable(generate-id())"/>
               <value-of select="$param"/>
               <text> => </text>
               <value-of select="$param, xcst:expression(@for)" separator="."/>
            </when>
            <otherwise>
               <value-of select="(@name/src:expand-attribute(.), src:string(''))[1]"/>
               <text>, </text>
               <value-of select="(@value/xcst:expression(.), 'default(object)')[1]"/>
            </otherwise>
         </choose>
         <if test="@format">
            <text>, format: </text>
            <value-of select="src:expand-attribute(@format)"/>
         </if>
         <variable name="merge-attributes" select="@html-type, @html-placeholder"/>
         <if test="not(empty((@html-attributes, @html-class, $merge-attributes)))">
            <text>, htmlAttributes: </text>
            <value-of select="a:html-attributes(@html-attributes, @html-class, $merge-attributes)"/>
         </if>
         <text>)</text>
      </variable>
      <c:void value="{$expr}"/>
   </template>

   <template match="a:password" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="$a:input-attributes, 'html-placeholder'"/>
         <with-param name="required" select="()"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for">
         <with-param name="attribs" select="@name, @value"/>
      </call-template>

      <variable name="expr">
         <value-of select="a:fully-qualified-helper-html('InputExtensions')"/>
         <text>.Password</text>
         <if test="@for">For</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <value-of select="$output"/>
         <text>, </text>
         <choose>
            <when test="@for">
               <variable name="param" select="src:aux-variable(generate-id())"/>
               <value-of select="$param"/>
               <text> => </text>
               <value-of select="$param, xcst:expression(@for)" separator="."/>
            </when>
            <otherwise>
               <value-of select="(@name/src:expand-attribute(.), src:string(''))[1]"/>
               <text>, </text>
               <value-of select="(@value/xcst:expression(.), 'default(object)')[1]"/>
            </otherwise>
         </choose>
         <variable name="merge-attributes" select="@html-placeholder"/>
         <if test="not(empty((@html-attributes, @html-class, $merge-attributes)))">
            <text>, htmlAttributes: </text>
            <value-of select="a:html-attributes(@html-attributes, @html-class, $merge-attributes)"/>
         </if>
         <text>)</text>
      </variable>
      <c:void value="{$expr}"/>
   </template>

   <template match="a:hidden" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="$a:input-attributes"/>
         <with-param name="required" select="()"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for">
         <with-param name="attribs" select="@name, @value"/>
      </call-template>

      <variable name="expr">
         <value-of select="a:fully-qualified-helper-html('InputExtensions')"/>
         <text>.Hidden</text>
         <if test="@for">For</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <value-of select="$output"/>
         <text>, </text>
         <choose>
            <when test="@for">
               <variable name="param" select="src:aux-variable(generate-id())"/>
               <value-of select="$param"/>
               <text> => </text>
               <value-of select="$param, xcst:expression(@for)" separator="."/>
            </when>
            <otherwise>
               <value-of select="(@name/src:expand-attribute(.), src:string(''))[1]"/>
               <text>, </text>
               <value-of select="(@value/xcst:expression(.), 'default(object)')[1]"/>
            </otherwise>
         </choose>
         <variable name="merge-attributes" select="()"/>
         <if test="not(empty((@html-attributes, @html-class, $merge-attributes)))">
            <text>, htmlAttributes: </text>
            <value-of select="a:html-attributes(@html-attributes, @html-class, $merge-attributes)"/>
         </if>
         <text>)</text>
      </variable>
      <c:void value="{$expr}"/>
   </template>

   <template match="a:text-area" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="$a:input-attributes, 'rows', 'cols', 'html-placeholder'"/>
         <with-param name="required" select="()"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for">
         <with-param name="attribs" select="@name, @value"/>
      </call-template>

      <variable name="expr">
         <value-of select="a:fully-qualified-helper-html('TextAreaExtensions')"/>
         <text>.TextArea</text>
         <if test="@for">For</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <value-of select="$output"/>
         <text>, </text>
         <choose>
            <when test="@for">
               <variable name="param" select="src:aux-variable(generate-id())"/>
               <value-of select="$param"/>
               <text> => </text>
               <value-of select="$param, xcst:expression(@for)" separator="."/>
            </when>
            <otherwise>
               <value-of select="(@name/src:expand-attribute(.), src:string(''))[1]"/>
               <text>, </text>
               <value-of select="(@value/xcst:expression(.), 'default(object)')[1]"/>
            </otherwise>
         </choose>
         <if test="@rows or @cols">
            <text>, rows: </text>
            <value-of select="(@rows/xcst:expression(.), '2')[1]"/>
            <text>, columns: </text>
            <value-of select="(@cols/xcst:expression(.), '20')[1]"/>
         </if>
         <variable name="merge-attributes" select="@html-placeholder"/>
         <if test="not(empty((@html-attributes, @html-class, $merge-attributes)))">
            <text>, htmlAttributes: </text>
            <value-of select="a:html-attributes(@html-attributes, @html-class, $merge-attributes)"/>
         </if>
         <text>)</text>
      </variable>
      <c:void value="{$expr}"/>
   </template>

   <template match="a:check-box" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="$a:html-attributes, 'for', 'name', 'checked'"/>
         <with-param name="required" select="()"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for">
         <with-param name="attribs" select="@name, @checked"/>
      </call-template>

      <variable name="expr">
         <value-of select="a:fully-qualified-helper-html('InputExtensions')"/>
         <text>.CheckBox</text>
         <if test="@for">For</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <value-of select="$output"/>
         <text>, </text>
         <choose>
            <when test="@for">
               <variable name="param" select="src:aux-variable(generate-id())"/>
               <value-of select="$param"/>
               <text> => </text>
               <value-of select="$param, xcst:expression(@for)" separator="."/>
            </when>
            <otherwise>
               <value-of select="(@name/src:expand-attribute(.), src:string(''))[1]"/>
            </otherwise>
         </choose>
         <if test="not(@for) and @checked">
            <text>, isChecked: </text>
            <value-of select="xcst:expression(@checked)"/>
         </if>
         <variable name="merge-attributes" select="()"/>
         <if test="not(empty((@html-attributes, @html-class, $merge-attributes)))">
            <text>, htmlAttributes: </text>
            <value-of select="a:html-attributes(@html-attributes, @html-class, $merge-attributes)"/>
         </if>
         <text>)</text>
      </variable>
      <c:void value="{$expr}"/>
   </template>

   <template match="a:radio-button" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="$a:html-attributes, 'for', 'name', 'value', 'checked'"/>
         <with-param name="required" select="'value'"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for">
         <with-param name="attribs" select="@name, @checked"/>
      </call-template>

      <variable name="expr">
         <value-of select="a:fully-qualified-helper-html('InputExtensions')"/>
         <text>.RadioButton</text>
         <if test="@for">For</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <value-of select="$output"/>
         <text>, </text>
         <choose>
            <when test="@for">
               <variable name="param" select="src:aux-variable(generate-id())"/>
               <value-of select="$param"/>
               <text> => </text>
               <value-of select="$param, xcst:expression(@for)" separator="."/>
            </when>
            <otherwise>
               <value-of select="(@name/src:expand-attribute(.), src:string(''))[1]"/>
            </otherwise>
         </choose>
         <text>, </text>
         <value-of select="xcst:expression(@value)"/>
         <if test="not(@for) and @checked">
            <text>, isChecked: </text>
            <value-of select="xcst:expression(@checked)"/>
         </if>
         <variable name="merge-attributes" select="()"/>
         <if test="not(empty((@html-attributes, @html-class, $merge-attributes)))">
            <text>, htmlAttributes: </text>
            <value-of select="a:html-attributes(@html-attributes, @html-class, $merge-attributes)"/>
         </if>
         <text>)</text>
      </variable>
      <c:void value="{$expr}"/>
   </template>

   <template match="a:anti-forgery-token" mode="src:extension-instruction">

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="()"/>
         <with-param name="required" select="()"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <variable name="expr">
         <value-of select="src:global-identifier('System.Web.Helpers.AntiForgery')"/>
         <text>.GetHtml().ToString()</text>
      </variable>
      <c:value-of value="{$expr}" disable-output-escaping="yes"/>
   </template>

   <template match="a:http-method-override" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'method'"/>
         <with-param name="required" select="'method'"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <variable name="expr">
         <value-of select="a:fully-qualified-helper-html('InputExtensions')"/>
         <text>.HttpMethodOverride(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <value-of select="$output"/>
         <text>, </text>
         <value-of select="src:expand-attribute(@method)"/>
         <text>)</text>
      </variable>
      <c:void value="{$expr}"/>
   </template>

   <template match="a:drop-down-list | a:list-box" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>

      <variable name="ddl" select="self::a:drop-down-list"/>
      <variable name="for-model" select="empty((@for, @name))"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="$a:input-attributes, 'options'"/>
         <with-param name="required" select="()"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for">
         <with-param name="attribs" select="@name, @value"/>
      </call-template>

      <variable name="expr">
         <value-of select="a:fully-qualified-helper-html('SelectExtensions')"/>
         <text>.</text>
         <value-of select="if ($ddl) then 'DropDownList' else 'ListBox'"/>
         <if test="@for or $for-model">For</if>
         <if test="$for-model">Model</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <value-of select="$output"/>
         <if test="not($for-model)">
            <text>, </text>
            <choose>
               <when test="@for">
                  <variable name="param" select="src:aux-variable(generate-id())"/>
                  <value-of select="$param"/>
                  <text> => </text>
                  <value-of select="$param, xcst:expression(@for)" separator="."/>
               </when>
               <when test="@name">
                  <value-of select="src:expand-attribute(@name)"/>
               </when>
            </choose>
         </if>
         <text>, </text>
         <call-template name="a:options">
            <with-param name="value" select="@value"/>
            <with-param name="allowMultiple" select="not($ddl)"/>
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         </call-template>
         <if test="$ddl and @option-label">
            <text>, optionLabel: </text>
            <value-of select="src:expand-attribute(@option-label)"/>
         </if>
         <variable name="merge-attributes" select="()"/>
         <if test="not(empty((@html-attributes, @html-class, $merge-attributes)))">
            <text>, htmlAttributes: </text>
            <value-of select="a:html-attributes(@html-attributes, @html-class, $merge-attributes)"/>
         </if>
         <text>)</text>
      </variable>
      <c:void value="{$expr}"/>
   </template>

   <template name="a:options">
      <param name="value" as="attribute()?"/>
      <param name="allowMultiple" select="false()"/>

      <choose>
         <when test="a:option or @options">
            <!--
               Casting of xcst:expression avoids turning into a dynamic object when one of the arguments is dynamic.
               A long method chain on a dynamic object hurts performance.
            -->
            <value-of select="a:fully-qualified-helper('OptionList')"/>
            <choose>
               <when test="a:option">
                  <text>.FromStaticList(</text>
                  <value-of select="src:integer(count(a:option))"/>
                  <text>)</text>
               </when>
               <otherwise>.Create()</otherwise>
            </choose>
            <if test="$value">
               <call-template name="src:line-number"/>
               <call-template name="src:new-line-indented"/>
               <choose>
                  <when test="$allowMultiple">
                     <text>.WithSelectedValues((</text>
                     <value-of select="src:global-identifier('System.Collections.IEnumerable')"/>
                     <text>)</text>
                  </when>
                  <otherwise>.WithSelectedValue((object)</otherwise>
               </choose>
               <text>(</text>
               <value-of select="xcst:expression($value)"/>
               <text>))</text>
            </if>
            <for-each select="a:option">
               <call-template name="xcst:validate-attribs">
                  <with-param name="allowed" select="'value', 'selected', 'disabled'"/>
                  <with-param name="required" select="()"/>
                  <with-param name="extension" select="true()"/>
               </call-template>
               <call-template name="src:line-number"/>
               <call-template name="src:new-line-indented"/>
               <text>.AddStaticOption(</text>
               <if test="@value">
                  <text>value: (object)(</text>
                  <value-of select="xcst:expression(@value)"/>
                  <text>), </text>
               </if>
               <text>text: </text>
               <call-template name="src:simple-content"/>
               <if test="@selected">
                  <text>, selected: (bool)(</text>
                  <value-of select="xcst:expression(@selected)"/>
                  <text>)</text>
               </if>
               <if test="@disabled">
                  <text>, disabled: (bool)(</text>
                  <value-of select="xcst:expression(@disabled)"/>
                  <text>)</text>
               </if>
               <text>)</text>
            </for-each>
            <if test="@options">
               <call-template name="src:line-number"/>
               <call-template name="src:new-line-indented"/>
               <!-- Don't cast expression, behavior depends on overload resolution -->
               <text>.ConcatDynamicList(</text>
               <value-of select="xcst:expression(@options)"/>
               <text>)</text>
            </if>
         </when>
         <otherwise>
            <value-of select="concat('default(', src:global-identifier('System.Collections.Generic.IEnumerable'), '&lt;', src:global-identifier('System.Web.Mvc.SelectListItem'), '>)')"/>
         </otherwise>
      </choose>
   </template>

   <template match="a:label" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="$a:html-attributes, 'for', 'name', 'text'"/>
         <with-param name="required" select="()"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for"/>

      <variable name="for-model" select="empty((@for, @name))"/>
      <variable name="expr">
         <value-of select="a:fully-qualified-helper-html('LabelExtensions')"/>
         <text>.Label</text>
         <choose>
            <when test="@for">For</when>
            <when test="$for-model">ForModel</when>
         </choose>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <value-of select="$output"/>
         <if test="not($for-model)">
            <text>, </text>
            <choose>
               <when test="@for">
                  <variable name="param" select="src:aux-variable(generate-id())"/>
                  <value-of select="$param"/>
                  <text> => </text>
                  <value-of select="$param, @for" separator="."/>
               </when>
               <when test="@name">
                  <value-of select="src:expand-attribute(@name)"/>
               </when>
            </choose>
         </if>
         <if test="@text">
            <text>, labelText: </text>
            <value-of select="src:expand-attribute(@text)"/>
         </if>
         <variable name="merge-attributes" select="()"/>
         <if test="not(empty((@html-attributes, @html-class, $merge-attributes)))">
            <text>, htmlAttributes: </text>
            <value-of select="a:html-attributes(@html-attributes, @html-class, $merge-attributes)"/>
         </if>
         <text>)</text>
      </variable>
      <c:void value="{$expr}"/>
   </template>

   <template match="a:validation-summary" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="$a:html-attributes, 'include-member-errors', 'message'"/>
         <with-param name="required" select="()"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <variable name="expr">
         <value-of select="a:fully-qualified-helper-html('ValidationExtensions')"/>
         <text>.ValidationSummary(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <value-of select="$output"/>
         <if test="@include-member-errors">
            <text>, includePropertyErrors: </text>
            <value-of select="xcst:expression(@include-member-errors)"/>
         </if>
         <text>, message: </text>
         <value-of select="(@message/src:expand-attribute(.), 'default(string)')[1]"/>
         <variable name="merge-attributes" select="()"/>
         <if test="not(empty((@html-attributes, @html-class, $merge-attributes)))">
            <text>, htmlAttributes: </text>
            <value-of select="a:html-attributes(@html-attributes, @html-class, $merge-attributes)"/>
         </if>
         <text>)</text>
      </variable>
      <c:void value="{$expr}"/>
   </template>

   <template match="a:validation-message" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="$a:html-attributes, 'for', 'name', 'message'"/>
         <with-param name="required" select="()"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for"/>

      <variable name="expr">
         <value-of select="a:fully-qualified-helper-html('ValidationExtensions')"/>
         <text>.ValidationMessage</text>
         <if test="@for">For</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <value-of select="$output"/>
         <text>, </text>
         <choose>
            <when test="@for">
               <variable name="param" select="src:aux-variable(generate-id())"/>
               <value-of select="$param"/>
               <text> => </text>
               <value-of select="$param, xcst:expression(@for)" separator="."/>
            </when>
            <when test="@name">
               <value-of select="src:expand-attribute(@name)"/>
            </when>
            <otherwise>
               <value-of select="src:string('')"/>
            </otherwise>
         </choose>
         <text>, </text>
         <value-of select="(@message/src:expand-attribute(.), 'default(string)')[1]"/>
         <variable name="merge-attributes" select="()"/>
         <if test="not(empty((@html-attributes, @html-class, $merge-attributes)))">
            <text>, htmlAttributes: </text>
            <value-of select="a:html-attributes(@html-attributes, @html-class, $merge-attributes)"/>
         </if>
         <text>)</text>
      </variable>
      <c:void value="{$expr}"/>
   </template>

   <!--
      ## Templates
   -->

   <template match="a:editor | a:display" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'for', 'name', 'template', 'html-field-name', 'html-attributes', 'with-params', 'options'"/>
         <with-param name="required" select="()"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for"/>

      <variable name="editor" select="self::a:editor"/>
      <variable name="for-model" select="empty((@for, @name))"/>
      <variable name="expr">
         <value-of select="a:fully-qualified-helper-html(concat((if ($editor) then 'Editor' else 'Display'), 'Extensions'))"/>
         <text>.</text>
         <value-of select="if ($editor) then 'Editor' else 'Display'"/>
         <if test="@for or $for-model">For</if>
         <if test="$for-model">Model</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <value-of select="$output"/>
         <if test="not($for-model)">
            <text>, </text>
            <choose>
               <when test="@for">
                  <variable name="param" select="src:aux-variable(generate-id())"/>
                  <value-of select="$param"/>
                  <text> => </text>
                  <value-of select="$param, xcst:expression(@for)" separator="."/>
               </when>
               <when test="@name">
                  <value-of select="src:expand-attribute(@name)"/>
               </when>
            </choose>
         </if>
         <text>, templateName: </text>
         <choose>
            <when test="@template">
               <value-of select="src:expand-attribute(@template)"/>
            </when>
            <otherwise>null</otherwise>
         </choose>
         <if test="@html-field-name">
            <text>, htmlFieldName: </text>
            <value-of select="src:expand-attribute(@html-field-name)"/>
         </if>
         <call-template name="a:editor-additional-view-data"/>
         <text>)</text>
      </variable>
      <c:void value="{$expr}"/>
   </template>

   <template name="a:editor-additional-view-data">
      <variable name="setters" as="text()*">
         <for-each select="@html-attributes, a:with-options, .[@options], a:member-template">
            <variable name="setter">
               <apply-templates select="." mode="a:editor-additional-view-data"/>
            </variable>
            <if test="string($setter)">
               <value-of select="$setter"/>
            </if>
         </for-each>
      </variable>
      <if test="@with-params or $setters">
         <text>, additionalViewData: new </text>
         <value-of select="src:global-identifier('System.Web.Routing.RouteValueDictionary')"/>
         <if test="@with-params">
            <text>(</text>
            <value-of select="xcst:expression(@with-params)"/>
            <text>)</text>
         </if>
         <text> { </text>
         <value-of select="string-join($setters, ', ')"/>
         <text> }</text>
      </if>
   </template>

   <template match="@html-attributes" mode="a:editor-additional-view-data">
      <text>[</text>
      <value-of select="src:string('htmlAttributes')"/>
      <text>] = </text>
      <value-of select="xcst:expression(.)"/>
   </template>

   <template match="a:with-options | a:*[@options]" mode="a:editor-additional-view-data">
      <param name="indent" tunnel="yes"/>

      <if test="self::a:with-options">
         <call-template name="xcst:validate-attribs">
            <with-param name="allowed" select="'for', 'name', 'options'"/>
            <with-param name="required" select="()"/>
            <with-param name="extension" select="true()"/>
         </call-template>
         <call-template name="a:validate-for">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         </call-template>
      </if>

      <text>[</text>
      <value-of select="src:string(concat(src:aux-variable('options'), ':'))"/>
      <text> + </text>
      <call-template name="a:model-helper"/>
      <text>.FieldName(</text>
      <choose>
         <when test="@for or self::a:with-options/../@for">
            <variable name="param" select="src:aux-variable(generate-id())"/>
            <value-of select="$param"/>
            <text> => </text>
            <value-of select="$param, self::a:with-options/../@for/xcst:expression(.), @for/xcst:expression(.)" separator="."/>
         </when>
         <otherwise>
            <value-of select="self::a:with-options/../@name/src:expand-attribute(.), @name/src:expand-attribute(.)" separator="."/>
         </otherwise>
      </choose>
      <text>)] = </text>
      <call-template name="a:options">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </call-template>
   </template>

   <template match="a:member-template" mode="a:editor-additional-view-data">
      <param name="indent" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'helper-name'"/>
         <with-param name="required" select="()"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <variable name="new-output" select="concat(src:aux-variable('output'), '_', generate-id())"/>
      <variable name="new-helper" select="(@helper-name/xcst:name(.), concat(src:aux-variable('model_helper'), '_', generate-id()))[1]"/>

      <text>[</text>
      <value-of select="src:string(src:aux-variable('member_template'))"/>
      <text>]</text>
      <text> = new </text>
      <value-of select="src:global-identifier(concat('System.Action&lt;', src:global-identifier('Xcst.Web.Mvc.ModelHelper'), ', ', src:global-identifier('Xcst.XcstWriter'), '>'))"/>
      <text>((</text>
      <value-of select="$new-helper, $new-output" separator=", "/>
      <text>) => </text>
      <call-template name="src:sequence-constructor">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         <with-param name="output" select="$new-output" tunnel="yes"/>
         <with-param name="a:model-helper" select="$new-helper" tunnel="yes"/>
      </call-template>
      <text>)</text>
   </template>

   <!--
      ## Metadata
   -->

   <template match="@display" mode="src:scaffold-column-attribute">
      <next-match/>
      <variable name="display" select="xcst:non-string(.)"/>
      <variable name="scaffold" select="if ($display = ('view-only', 'edit-only')) then true() else xcst:boolean(.)"/>
      <if test="$scaffold">
         <call-template name="src:line-hidden"/>
         <call-template name="src:new-line-indented"/>
         <text>[</text>
         <value-of select="src:global-identifier('System.Web.Mvc.AdditionalMetadata')"/>
         <text>("ShowForDisplay", </text>
         <value-of select="src:boolean($display ne 'edit-only')"/>
         <text>)]</text>
         <call-template name="src:new-line-indented"/>
         <text>[</text>
         <value-of select="src:global-identifier('System.Web.Mvc.AdditionalMetadata')"/>
         <text>("ShowForEdit", </text>
         <value-of select="src:boolean($display ne 'view-only')"/>
         <text>)]</text>
      </if>
   </template>

   <template match="c:member" mode="src:display-attribute">
      <next-match/>
      <if test="@group">
         <call-template name="src:line-hidden"/>
         <call-template name="src:new-line-indented"/>
         <text>[</text>
         <value-of select="src:global-identifier('System.Web.Mvc.AdditionalMetadata')"/>
         <text>("GroupName", </text>
         <value-of select="src:verbatim-string(@group)"/>
         <text>)]</text>
      </if>
   </template>

   <template match="a:display-name" mode="src:extension-instruction">

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'for', 'name'"/>
         <with-param name="required" select="()"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for"/>

      <variable name="for-model" select="empty((@for, @name))"/>
      <variable name="expr">
         <call-template name="a:model-helper"/>
         <text>.DisplayName(</text>
         <if test="not($for-model)">
            <choose>
               <when test="@for">
                  <variable name="param" select="src:aux-variable(generate-id())"/>
                  <value-of select="$param"/>
                  <text> => </text>
                  <value-of select="$param, xcst:expression(@for)" separator="."/>
               </when>
               <when test="@name">
                  <value-of select="src:expand-attribute(@name)"/>
               </when>
            </choose>
         </if>
         <text>)</text>
      </variable>
      <c:object value="{$expr}"/>
   </template>

   <template match="a:display-text" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>
      <param name="src:current-mode" as="xs:QName" required="yes" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'for', 'name'"/>
         <with-param name="required" select="()"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for"/>

      <variable name="statement" select="$src:current-mode eq xs:QName('src:statement')"/>

      <variable name="expr">
         <value-of select="a:fully-qualified-helper-html('DisplayTextExtensions')"/>
         <text>.Display</text>
         <value-of select="if ($statement) then 'Text' else 'String'"/>
         <if test="@for">For</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <if test="$statement">
            <text>, </text>
            <value-of select="$output"/>
         </if>
         <text>, </text>
         <choose>
            <when test="@for">
               <variable name="param" select="src:aux-variable(generate-id())"/>
               <value-of select="$param"/>
               <text> => </text>
               <value-of select="$param, xcst:expression(@for)" separator="."/>
            </when>
            <otherwise>
               <value-of select="(@name/src:expand-attribute(.), src:string(''))[1]"/>
            </otherwise>
         </choose>
         <text>)</text>
      </variable>
      <choose>
         <when test="$statement">
            <c:void value="{$expr}"/>
         </when>
         <otherwise>
            <c:object value="{$expr}"/>
         </otherwise>
      </choose>
   </template>

   <!--
      ## Models
   -->

   <template match="a:model" mode="src:extension-instruction">

      <call-template name="xcst:validate-attribs">
         <with-param name="allowed" select="'value', 'as', 'html-field-prefix', 'helper-name', 'with-params'"/>
         <with-param name="required" select="()"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <if test="not(@value or @as)">
         <sequence select="error((), 'Element must have either @value or @as attributes.', src:error-object(.))"/>
      </if>

      <variable name="new-helper" select="(@helper-name/xcst:name(.), concat(src:aux-variable('model_helper'), '_', generate-id()))[1]"/>
      <variable name="type" select="@as/xcst:type(.)"/>
      <call-template name="src:new-line-indented"/>
      <text>var </text>
      <value-of select="$new-helper"/>
      <text> = </text>
      <value-of select="src:global-identifier('Xcst.Web.Mvc.ModelHelper')"/>
      <text>.ForModel</text>
      <if test="$type">
         <value-of select="concat('&lt;', $type, '>')"/>
      </if>
      <text>(</text>
      <call-template name="a:model-helper"/>
      <text>, </text>
      <value-of select="(@value/xcst:expression(.), concat('default(', ($type, 'object')[1], ')'))[1]"/>
      <if test="@html-field-prefix">
         <text>, htmlFieldPrefix: </text>
         <value-of select="src:expand-attribute(@html-field-prefix)"/>
      </if>
      <if test="@with-params">
         <text>, additionalViewData: </text>
         <value-of select="xcst:expression(@with-params)"/>
      </if>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
      <call-template name="src:sequence-constructor">
         <with-param name="ensure-block" select="true()"/>
         <with-param name="a:model-helper" select="$new-helper" tunnel="yes"/>
      </call-template>
   </template>

   <!--
      ## Infrastructure
   -->

   <template match="c:module | c:package" mode="src:import-namespace-extra">
      <param name="class" tunnel="yes"/>
      <param name="library-package" tunnel="yes"/>

      <next-match/>
      <if test="not($library-package)">
         <call-template name="src:new-line-indented"/>
         <text>using static </text>
         <value-of select="$class, a:functions-type-name(.)" separator="."/>
         <value-of select="$src:statement-delimiter"/>
      </if>
   </template>

   <template match="c:module | c:package" mode="src:infrastructure-extra">
      <param name="indent" tunnel="yes"/>
      <param name="library-package" tunnel="yes"/>

      <next-match/>
      <if test="not($library-package)">
         <variable name="module-uri" select="document-uri(root(.))"/>
         <variable name="functions-type" select="a:functions-type-name(.)"/>
         <value-of select="$src:new-line"/>
         <call-template name="src:new-line-indented"/>
         <text>internal static class </text>
         <value-of select="$functions-type"/>
         <call-template name="src:open-brace"/>
         <value-of select="$src:new-line"/>
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="1"/>
         </call-template>
         <text>static readonly string BasePath = </text>
         <value-of select="src:global-identifier('System.Web.VirtualPathUtility')"/>
         <text>.ToAbsolute(</text>
         <value-of select="src:verbatim-string(concat('~/', src:make-relative-uri($a:application-uri, $module-uri)))"/>
         <text>)</text>
         <value-of select="$src:statement-delimiter"/>
         <value-of select="$src:new-line"/>
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="1"/>
         </call-template>
         <text>public static string Href(string path, params object[] pathParts)</text>
         <call-template name="src:open-brace"/>
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="2"/>
         </call-template>
         <text>return </text>
         <value-of select="src:global-identifier('Xcst.Web.Runtime'), 'UrlUtil'" separator="."/>
         <text>.GenerateClientUrl(</text>
         <value-of select="$functions-type, 'BasePath'" separator="."/>
         <text>, path, pathParts)</text>
         <value-of select="$src:statement-delimiter"/>
         <call-template name="src:close-brace">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         </call-template>
         <call-template name="src:close-brace"/>
      </if>
   </template>

   <function name="a:functions-type-name">
      <param name="module" as="element()"/>

      <sequence select="concat('__xcst_functions_', generate-id($module))"/>
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

   <template name="a:model-helper">
      <param name="a:model-helper" as="xs:string?" tunnel="yes"/>

      <choose>
         <when test="$a:model-helper">
            <value-of select="$a:model-helper"/>
         </when>
         <otherwise>
            <text>((</text>
            <value-of select="src:global-identifier('Xcst.Web.Mvc.XcstViewPage')"/>
            <text>)</text>
            <value-of select="$src:context-field"/>
            <text>.TopLevelPackage).ModelHelper</text>
         </otherwise>
      </choose>
   </template>

   <template name="a:html-helper">
      <variable name="model-helper">
         <call-template name="a:model-helper"/>
      </variable>
      <value-of select="$model-helper, 'Html'" separator="."/>
   </template>

   <function name="a:html-attributes" as="xs:string">
      <param name="html-attributes" as="attribute()?"/>
      <param name="html-class" as="attribute()?"/>
      <param name="merge-attributes" as="attribute()*"/>

      <variable name="expr">
         <value-of select="a:fully-qualified-helper-html('HtmlAttributesMerger')"/>
         <text>.Create(</text>
         <value-of select="$html-attributes/xcst:expression(.)"/>
         <text>)</text>
         <if test="$html-class">
            <text>.AddCssClass(</text>
            <value-of select="src:expand-attribute($html-class)"/>
            <text>)</text>
         </if>
         <for-each select="$merge-attributes">
            <text>.AddDontReplace(</text>
            <value-of select="src:string(substring(local-name(), 6))"/>
            <text>, </text>
            <value-of select="src:expand-attribute(.)"/>
            <text>)</text>
         </for-each>
         <text>.Attributes</text>
      </variable>
      <sequence select="string($expr)"/>
   </function>

   <function name="a:fully-qualified-helper" as="xs:string">
      <param name="helper" as="xs:string"/>

      <sequence select="concat(src:global-identifier('Xcst.Web.Runtime'), '.', $helper)"/>
   </function>

   <function name="a:fully-qualified-helper-html" as="xs:string">
      <param name="helper" as="xs:string"/>

      <sequence select="concat(src:global-identifier('Xcst.Web.Mvc.Html'), '.', $helper)"/>
   </function>

</stylesheet>
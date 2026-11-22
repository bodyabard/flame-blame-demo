const fs=require('fs'),crypto=require('crypto'),zlib=require('zlib');
const id=()=>crypto.randomBytes(16).toString('hex');
const src=fs.readFileSync('Assets/TextMesh Pro/Shaders/TMP_SDF-URP Lit.shadergraph','utf8');let dec=new TextDecoder();const objs=src.split(/\r?\n\r?\n/).filter(s=>s.trim()).map(s=>JSON.parse(s));const old=objs[0], by=new Map(objs.map(o=>[o.m_ObjectId,o]));const all=[];const add=o=>(o.m_ObjectId=id(),all.push(o),o);const ref=o=>({m_Id:o.m_ObjectId});
const graph=JSON.parse(JSON.stringify(old));graph.m_ObjectId=id(); graph.m_Properties=[];graph.m_Nodes=[];graph.m_Edges=[];graph.m_Keywords=[];graph.m_Dropdowns=[];graph.m_CategoryData=[];graph.m_GroupDatas=[];graph.m_StickyNoteDatas=[];graph.m_Path='FlameBlame';
for(const t of objs.filter(o=>/Universal(Target|LitSubTarget)$/.test(o.m_Type))){all.push(t);if(t.m_Type.endsWith('UniversalTarget')){t.m_SurfaceType=0;t.m_AllowMaterialOverride=true;t.m_RenderFace=2;t.m_CastShadows=false;t.m_CustomEditorGUI='';}}
function retain(refs){for(const r of refs||[]){const o=by.get(r.m_Id);if(o&&!all.includes(o)){all.push(o);retain(o.m_Slots);}}}
retain(old.m_VertexContext.m_Blocks);retain(old.m_FragmentContext.m_Blocks);
const reflectionKeyword=add({m_SGVersion:0,m_Type:'UnityEditor.ShaderGraph.ShaderKeyword',m_Name:'Environment Reflections Off',m_Guid:{m_GuidSerialized:id()},m_DefaultReferenceName:'_ENVIRONMENTREFLECTIONS_OFF',m_OverrideReferenceName:'_ENVIRONMENTREFLECTIONS_OFF',m_GeneratePropertyBlock:false,m_KeywordType:0,m_KeywordDefinition:0,m_KeywordScope:1,m_KeywordStages:0,m_Value:1,m_IsEditable:true});graph.m_Keywords.push(ref(reflectionKeyword));
const blocks=all.filter(o=>o.m_Type.endsWith('.BlockNode'));graph.m_Nodes=blocks.map(ref);
const slot=(type,n,name,dir,val)=>add({m_SGVersion:0,m_Type:'UnityEditor.ShaderGraph.'+type+'MaterialSlot',m_Id:n,m_DisplayName:name,m_SlotType:dir,m_Hidden:false,m_ShaderOutputName:name,m_StageCapability:2,m_Value:val,m_DefaultValue:val,m_Labels:[]});
const fn=add({m_SGVersion:1,m_Type:'UnityEditor.ShaderGraph.CustomFunctionNode',m_Name:'PuddleToon',m_DrawState:{m_Expanded:true,m_Position:{x:0,y:0,width:220,height:350}},m_Slots:[],m_Precision:1,m_SourceType:1,m_FunctionName:'PuddleSurface',m_FunctionBody:fs.readFileSync('Tools/puddle-body.hlsl','utf8').trim().replace(/^\\uFEFF/,'').replace(/\\r/g,'')});graph.m_Nodes.push(ref(fn));
function input(s){fn.m_Slots.push(ref(s));return s;}
input(slot('UV',0,'UV',0,{x:0,y:0,z:0,w:0})).m_Channel=0;
input(slot('Position',1,'World',0,{x:0,y:0,z:0})).m_Space=0;
function edge(a,sa,b,sb){graph.m_Edges.push({m_OutputSlot:{m_Node:ref(a),m_SlotId:sa},m_InputSlot:{m_Node:ref(b),m_SlotId:sb}});}
let k=2;
for(const [name,value] of [['_Fill',1],['_Seed',0],['_MaskIndex',0],['_Electrified',0],['_Steam',0],['_EdgeNoise',.12],['_EdgeSpeed',.035],['_GlintSpeed',.045],['_RimWidth',.045]]){
const base=objs.find(o=>o.m_Type.endsWith('.Vector1ShaderProperty'));const p=add(JSON.parse(JSON.stringify(base)));p.m_Name=name;p.m_DefaultReferenceName=name;p.m_OverrideReferenceName=name;p.m_Value=value;p.m_FloatType=0;p.m_Guid={m_GuidSerialized:id()};p.m_GeneratePropertyBlock=true;p.m_Hidden=false;graph.m_Properties.push(ref(p));
const s=slot('Vector1',0,'Out',1,0);const node=add({m_SGVersion:0,m_Type:'UnityEditor.ShaderGraph.PropertyNode',m_Name:name,m_Slots:[ref(s)],m_Property:ref(p),m_Precision:0});graph.m_Nodes.push(ref(node));input(slot('Vector1',k,name.slice(1),0,value));edge(node,0,fn,k++);
}
for(const [name,r,g,b] of [['_WaterColor',.16,.57,.64],['_DeepColor',.12,.43,.56],['_RimColor',.7,.94,.91]]) {
 const p=add(JSON.parse(JSON.stringify(objs.find(o=>o.m_Type.endsWith('.ColorShaderProperty')))));
 p.m_Name=name;p.m_DefaultReferenceName=name;p.m_OverrideReferenceName=name;p.m_Value={r,g,b,a:1};p.m_Guid={m_GuidSerialized:id()};p.m_GeneratePropertyBlock=true;graph.m_Properties.push(ref(p));
 const s=slot('Vector4',0,'Out',1,{x:r,y:g,z:b,w:1});const n=add({m_SGVersion:0,m_Type:'UnityEditor.ShaderGraph.PropertyNode',m_Name:name,m_Slots:[ref(s)],m_Property:ref(p)});graph.m_Nodes.push(ref(n));input(slot('Vector3',k,name.slice(1),0,{x:r,y:g,z:b}));edge(n,0,fn,k++);
}
// Texture properties are sampled in the custom function through explicit texture inputs.
for(const name of ['_Mask0','_Mask1','_Mask2','_Mask3','_Noise']){
const base=objs.find(o=>o.m_Type.endsWith('.Texture2DShaderProperty'));const p=add(JSON.parse(JSON.stringify(base)));p.m_Name=name;p.m_DefaultReferenceName=name;p.m_OverrideReferenceName=name;p.m_Guid={m_GuidSerialized:id()};p.m_Value={m_SerializedTexture:'',m_Guid:''};p.m_GeneratePropertyBlock=true;graph.m_Properties.push(ref(p));const s=slot('Texture2D',0,'Out',1,undefined);const node=add({m_SGVersion:0,m_Type:'UnityEditor.ShaderGraph.PropertyNode',m_Name:name,m_Slots:[ref(s)],m_Property:ref(p)});graph.m_Nodes.push(ref(node));input(slot('Texture2D',k,name.slice(1),0,undefined));edge(node,0,fn,k++);
}
for(const [desc,type,name] of [['BaseColor','Vector3','Color'],['Emission','Vector3','Glow'],['Alpha','Vector1','Alpha'],['AlphaClipThreshold','Vector1','Threshold']]){const s=slot(type,k,name,1,type==='Vector3'?{x:0,y:0,z:0}:0);fn.m_Slots.push(ref(s));const b=blocks.find(b=>b.m_SerializedDescriptor==='SurfaceDescription.'+desc);if(b)edge(fn,k,b,by.get(b.m_Slots[0].m_Id).m_Id);k++;}
for(const b of blocks){const s=by.get(b.m_Slots[0].m_Id);if(b.m_SerializedDescriptor==='SurfaceDescription.Smoothness')s.m_Value=.68;}
for(const [kind,inputIndex] of [['UVNode',0],['PositionNode',1]]) {
 const original=objs.find(o=>o.m_Type.endsWith('.'+kind))||{m_SGVersion:0,m_Type:'UnityEditor.ShaderGraph.'+kind,m_Name:kind,m_Channel:0};
 const n=add(JSON.parse(JSON.stringify(original)));n.m_Slots=[];
 const s=slot(kind==='UVNode'?'Vector4':'Vector3',0,'Out',1,kind==='UVNode'?{x:0,y:0,z:0,w:0}:{x:0,y:0,z:0});n.m_Slots.push(ref(s));
 if(kind==='PositionNode')n.m_Space=2;
 graph.m_Nodes.push(ref(n));edge(n,0,fn,inputIndex);
}
fs.writeFileSync('Assets/_Project/Shaders/PuddleToon.shadergraph',[graph,...all].map(o=>JSON.stringify(o,null,4)).join('\n\n'));






